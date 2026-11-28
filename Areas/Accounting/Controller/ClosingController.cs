using System.Security.Claims;

using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.Areas.Accounting.Services;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    public record FiscalYearDto(int EndYear, string Label, DateOnly From, DateOnly To, decimal NetProfit, bool Ended, bool Closed, int? ClosingEntryId, string? ClosedBy, DateTime? ClosedAt);

    public record ClosingStatusDto(DateOnly? LockDate, List<FiscalYearDto> FiscalYears);

    public class LockDateInput
    {
        /// <summary>Null opens every period.</summary>
        public DateOnly? LockDate { get; set; }
    }

    /// <summary>Period lock date and Australian financial year closing (1 July – 30 June).</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/closing")]
    public class ClosingController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public ClosingController(ApplicationDBContext db) => _db = db;

        private static readonly AccountType[] PlTypes = { AccountType.Revenue, AccountType.OtherIncome, AccountType.DirectCosts, AccountType.Expense };

        [HttpGet]
        public async Task<ActionResult<ClosingStatusDto>> Get(int companyId)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var first = await _db.JournalEntries.Where(e => e.CompanyClientId == companyId).MinAsync(e => (DateOnly?)e.Date);
            var closings = await _db.JournalEntries.AsNoTracking().Where(e => e.CompanyClientId == companyId && e.Source == JournalSource.Closing)
                .Select(e => new { e.Id, e.Date, e.CreatedBy, e.CreatedAt }).ToListAsync();

            var years = new List<FiscalYearDto>();
            var currentEnd = ReportsController.FinancialYearStart(today).Year + 1;
            var firstEnd = first is null ? currentEnd : ReportsController.FinancialYearStart(first.Value).Year + 1;
            for (var end = currentEnd; end >= firstEnd; end--)
            {
                var (from, to) = Range(end);
                var net = await NetProfit(companyId, from, to);
                var c = closings.FirstOrDefault(x => x.Date == to);
                years.Add(new FiscalYearDto(end, Label(end), from, to, net, to < today, c != null, c?.Id, c?.CreatedBy, c?.CreatedAt));
            }
            return new ClosingStatusDto(await AccountingService.LockDateAsync(_db, companyId), years);
        }

        [HttpPut("lock-date")]
        public async Task<ActionResult<ClosingStatusDto>> SetLockDate(int companyId, LockDateInput input)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            var settings = await Settings(companyId);
            var old = settings.LockDate;
            settings.LockDate = input.LockDate;
            if (old != input.LockDate)
                Audit.Log(_db, User, companyId, input.LockDate is null ? "PeriodUnlocked" : "PeriodLocked", "Settings", companyId,
                    input.LockDate is null ? $"Períodos abiertos (antes cerrado hasta {old:dd/MM/yyyy})"
                                           : $"Períodos cerrados hasta el {input.LockDate:dd/MM/yyyy}{(old != null ? $" (antes {old:dd/MM/yyyy})" : "")}");
            await _db.SaveChangesAsync();
            return await Get(companyId);
        }

        /// <summary>
        /// Posts the closing entry on 30 June: zeroes every income/expense account for the year against retained earnings,
        /// and (by default) locks the period up to that date.
        /// </summary>
        [HttpPost("fiscal-years/{endYear:int}/close")]
        public async Task<ActionResult<ClosingStatusDto>> Close(int companyId, int endYear, [FromQuery] bool lockPeriod = true)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            var (from, to) = Range(endYear);
            if (to >= DateOnly.FromDateTime(DateTime.Today)) return Problem($"El año fiscal {Label(endYear)} aún no termina.", statusCode: 400);
            if (await _db.JournalEntries.AnyAsync(e => e.CompanyClientId == companyId && e.Source == JournalSource.Closing && e.Date == to))
                return Problem($"El año fiscal {Label(endYear)} ya está cerrado.", statusCode: 409);
            try { await AccountingService.EnsureOpenAsync(_db, companyId, to); }
            catch (AccountingException ex) { return Problem(ex.Message, statusCode: ex.StatusCode); }

            var balances = await _db.JournalLines.AsNoTracking()
                .Where(l => l.JournalEntry!.CompanyClientId == companyId && l.JournalEntry.Date >= from && l.JournalEntry.Date <= to
                            && l.JournalEntry.Source != JournalSource.Closing && PlTypes.Contains(l.Account!.Type))
                .GroupBy(l => l.AccountId)
                .Select(g => new { AccountId = g.Key, Net = g.Sum(l => l.Debit - l.Credit) })
                .ToListAsync();
            balances = balances.Where(b => b.Net != 0).ToList();
            if (balances.Count == 0) return Problem($"No hay ingresos ni gastos en {Label(endYear)}; no hay nada que cerrar.", statusCode: 400);

            using var trx = await _db.Database.BeginTransactionAsync();
            var retained = await AccountingService.EnsureRetainedEarningsAsync(_db, companyId);
            await _db.SaveChangesAsync();
            var profit = -balances.Sum(b => b.Net); // credit-positive: income > expenses
            var entry = new JournalEntry
            {
                CompanyClientId = companyId,
                Date = to,
                Narration = $"Cierre del año fiscal {Label(endYear)}",
                Source = JournalSource.Closing,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedBy = Audit.ActorName(User),
                Lines = balances.Select(b => new JournalLine
                {
                    AccountId = b.AccountId,
                    Description = "Cierre",
                    Debit = b.Net < 0 ? -b.Net : 0,
                    Credit = b.Net > 0 ? b.Net : 0,
                }).ToList(),
            };
            entry.Lines.Add(new JournalLine
            {
                AccountId = retained.Id,
                Description = profit >= 0 ? "Utilidad del ejercicio" : "Pérdida del ejercicio",
                Debit = profit < 0 ? -profit : 0,
                Credit = profit > 0 ? profit : 0,
            });
            _db.JournalEntries.Add(entry);

            var settings = await Settings(companyId);
            var locked = lockPeriod && (settings.LockDate is null || settings.LockDate < to);
            if (locked) settings.LockDate = to;
            await _db.SaveChangesAsync();
            Audit.Log(_db, User, companyId, "YearClosed", "JournalEntry", entry.Id,
                $"Año fiscal {Label(endYear)} cerrado: resultado {profit:0.00} a {retained.Code} {retained.Name}{(locked ? $"; períodos cerrados hasta {to:dd/MM/yyyy}" : "")}");
            await _db.SaveChangesAsync();
            await trx.CommitAsync();
            return await Get(companyId);
        }

        /// <summary>Reopens a year: deletes its closing entry. The period must be unlocked first (lock date before 30 June).</summary>
        [HttpDelete("fiscal-years/{endYear:int}")]
        public async Task<ActionResult<ClosingStatusDto>> Reopen(int companyId, int endYear)
        {
            var (_, to) = Range(endYear);
            var entry = await _db.JournalEntries.Include(e => e.Lines).ThenInclude(l => l.Account)
                .FirstOrDefaultAsync(e => e.CompanyClientId == companyId && e.Source == JournalSource.Closing && e.Date == to);
            if (entry is null) return Problem($"El año fiscal {Label(endYear)} no está cerrado.", statusCode: 404);
            try { await AccountingService.EnsureOpenAsync(_db, companyId, to); }
            catch (AccountingException ex) { return Problem(ex.Message + " Para reabrir el año, primero mueve la fecha de cierre a antes del 30 de junio.", statusCode: ex.StatusCode); }

            Audit.Log(_db, User, companyId, "YearReopened", "JournalEntry", entry.Id, $"Año fiscal {Label(endYear)} reabierto (asiento de cierre #{entry.Id} eliminado)", Audit.Snapshot(entry));
            _db.JournalEntries.Remove(entry);
            await _db.SaveChangesAsync();
            return await Get(companyId);
        }

        private static (DateOnly From, DateOnly To) Range(int endYear) => (new DateOnly(endYear - 1, 7, 1), new DateOnly(endYear, 6, 30));
        private static string Label(int endYear) => $"{endYear - 1}-{endYear % 100:00}";

        private async Task<decimal> NetProfit(int companyId, DateOnly from, DateOnly to) =>
            await _db.JournalLines.Where(l => l.JournalEntry!.CompanyClientId == companyId && l.JournalEntry.Date >= from && l.JournalEntry.Date <= to
                                             && l.JournalEntry.Source != JournalSource.Closing && PlTypes.Contains(l.Account!.Type))
                .SumAsync(l => (decimal?)(l.Credit - l.Debit)) ?? 0;

        private async Task<CompanyAccountingSettings> Settings(int companyId)
        {
            var s = await _db.AccountingSettings.FirstOrDefaultAsync(x => x.CompanyClientId == companyId);
            if (s is null)
            {
                s = new CompanyAccountingSettings { CompanyClientId = companyId };
                _db.AccountingSettings.Add(s);
            }
            return s;
        }
    }
}
