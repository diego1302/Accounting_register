using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    /// <summary>Financial reports computed from the posted journals of one company client.</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/reports")]
    public class ReportsController : ControllerBase
    {
        /// <summary>Upper bound of ledger lines returned in one request (the UI pages by period / account).</summary>
        private const int MaxLedgerLines = 5000;

        private readonly ApplicationDBContext _db;

        public ReportsController(ApplicationDBContext db) => _db = db;

        public static bool IsDebitNormal(AccountType t) => t is AccountType.Bank or AccountType.CurrentAsset or AccountType.NonCurrentAsset
            or AccountType.DirectCosts or AccountType.Expense;

        private static decimal Natural(AccountType t, decimal debit, decimal credit) => IsDebitNormal(t) ? debit - credit : credit - debit;

        /// <summary>Australian financial year (1 July – 30 June) containing the date.</summary>
        public static DateOnly FinancialYearStart(DateOnly d) => new(d.Month >= 7 ? d.Year : d.Year - 1, 7, 1);

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        [HttpGet("trial-balance")]
        public async Task<ActionResult<TrialBalanceDto>> TrialBalance(int companyId, [FromQuery] DateOnly? asOf)
        {
            if (!await CompanyExists(companyId)) return CompanyNotFound();
            var date = asOf ?? Today;
            var rows = (await Balances(companyId, null, date))
                .Where(b => b.Debit != b.Credit)
                .Select(b =>
                {
                    var net = b.Debit - b.Credit;
                    return new TrialBalanceRow(b.Account.Id, b.Account.Code, b.Account.Name, b.Account.Type, net > 0 ? net : 0, net < 0 ? -net : 0);
                })
                .OrderBy(r => r.Code).ToList();
            return new TrialBalanceDto(date, rows, rows.Sum(r => r.Debit), rows.Sum(r => r.Credit));
        }

        [HttpGet("profit-and-loss")]
        public async Task<ActionResult<ProfitAndLossDto>> ProfitAndLoss(int companyId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            if (!await CompanyExists(companyId)) return CompanyNotFound();
            var end = to ?? Today;
            var start = from ?? FinancialYearStart(end);
            if (start > end) return Problem("La fecha inicial es posterior a la final.", statusCode: 400);

            // The year-end closing entry zeroes income/expense accounts; the P&L must show the year's activity.
            var balances = await Balances(companyId, start, end, excludeClosing: true);
            ReportSection Section(string key, string title, params AccountType[] types)
            {
                var lines = balances.Where(b => types.Contains(b.Account.Type))
                    .Select(b => new ReportLine(b.Account.Id, b.Account.Code, b.Account.Name, Natural(b.Account.Type, b.Debit, b.Credit)))
                    .Where(l => l.Amount != 0).OrderBy(l => l.Code).ToList();
                return new ReportSection(key, title, lines, lines.Sum(l => l.Amount));
            }

            var income = Section("income", "Ingresos", AccountType.Revenue);
            var cos = Section("costOfSales", "Costo de ventas", AccountType.DirectCosts);
            var other = Section("otherIncome", "Otros ingresos", AccountType.OtherIncome);
            var expenses = Section("expenses", "Gastos de operación", AccountType.Expense);
            var gross = income.Total - cos.Total;
            return new ProfitAndLossDto(start, end, income, cos, gross, other, expenses, gross + other.Total - expenses.Total);
        }

        /// <summary>
        /// Balance sheet at a date. There is no year-end closing yet, so profit is shown on two computed equity lines:
        /// current financial year (from 1 July) and earlier years.
        /// </summary>
        [HttpGet("balance-sheet")]
        public async Task<ActionResult<BalanceSheetDto>> BalanceSheet(int companyId, [FromQuery] DateOnly? asOf)
        {
            if (!await CompanyExists(companyId)) return CompanyNotFound();
            var date = asOf ?? Today;
            var fyStart = FinancialYearStart(date);
            var balances = await Balances(companyId, null, date);

            ReportSection Section(string key, string title, params AccountType[] types)
            {
                var lines = balances.Where(b => types.Contains(b.Account.Type))
                    .Select(b => new ReportLine(b.Account.Id, b.Account.Code, b.Account.Name, Natural(b.Account.Type, b.Debit, b.Credit)))
                    .Where(l => l.Amount != 0).OrderBy(l => l.Code).ToList();
                return new ReportSection(key, title, lines, lines.Sum(l => l.Amount));
            }

            var assets = new List<ReportSection>
            {
                Section("bank", "Bancos", AccountType.Bank),
                Section("currentAssets", "Activo corriente", AccountType.CurrentAsset),
                Section("nonCurrentAssets", "Activo no corriente", AccountType.NonCurrentAsset),
            };
            var liabilities = new List<ReportSection>
            {
                Section("currentLiabilities", "Pasivo corriente", AccountType.CurrentLiability),
                Section("nonCurrentLiabilities", "Pasivo no corriente", AccountType.NonCurrentLiability),
            };

            var plTypes = new[] { AccountType.Revenue, AccountType.OtherIncome, AccountType.DirectCosts, AccountType.Expense };
            var currentYear = (await Balances(companyId, fyStart, date)).Where(b => plTypes.Contains(b.Account.Type))
                .Sum(b => b.Credit - b.Debit);
            var allTime = balances.Where(b => plTypes.Contains(b.Account.Type)).Sum(b => b.Credit - b.Debit);

            var equity = Section("equity", "Patrimonio", AccountType.Equity);
            var equityLines = equity.Lines.ToList();
            if (allTime - currentYear != 0) equityLines.Add(new ReportLine(null, null, "Resultados de ejercicios anteriores", allTime - currentYear));
            if (currentYear != 0) equityLines.Add(new ReportLine(null, null, "Resultado del ejercicio", currentYear));
            equity = equity with { Lines = equityLines, Total = equityLines.Sum(l => l.Amount) };

            var totalAssets = assets.Sum(s => s.Total);
            var totalLiabilities = liabilities.Sum(s => s.Total);
            return new BalanceSheetDto(date, fyStart,
                assets.Where(s => s.Lines.Count > 0).ToList(), totalAssets,
                liabilities.Where(s => s.Lines.Count > 0).ToList(), totalLiabilities,
                totalAssets - totalLiabilities, equity, equity.Total);
        }

        /// <summary>Movements per account with opening balance and running balance. Without accountId: every account with activity.</summary>
        [HttpGet("general-ledger")]
        public async Task<ActionResult<GeneralLedgerDto>> GeneralLedger(int companyId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? accountId)
        {
            if (!await CompanyExists(companyId)) return CompanyNotFound();
            var end = to ?? Today;
            var start = from ?? new DateOnly(end.Year, end.Month, 1);
            if (start > end) return Problem("La fecha inicial es posterior a la final.", statusCode: 400);

            var accounts = await _db.Accounts.AsNoTracking().Where(a => a.CompanyClientId == companyId && (accountId == null || a.Id == accountId))
                .ToDictionaryAsync(a => a.Id);
            if (accountId != null && accounts.Count == 0) return NotFound();

            var opening = (await Balances(companyId, null, start.AddDays(-1), accountId)).ToDictionary(b => b.Account.Id);

            var lines = await _db.JournalLines.AsNoTracking()
                .Where(l => l.JournalEntry!.CompanyClientId == companyId && l.JournalEntry.Date >= start && l.JournalEntry.Date <= end
                            && (accountId == null || l.AccountId == accountId))
                .OrderBy(l => l.JournalEntry!.Date).ThenBy(l => l.JournalEntryId).ThenBy(l => l.Id)
                .Select(l => new
                {
                    l.AccountId, l.JournalEntryId, l.JournalEntry!.Date, l.JournalEntry.Reference, l.JournalEntry.Narration,
                    l.Description, l.JournalEntry.Source, l.Debit, l.Credit,
                })
                .Take(MaxLedgerLines + 1).ToListAsync();
            var truncated = lines.Count > MaxLedgerLines;
            if (truncated) lines.RemoveAt(lines.Count - 1);

            var byAccount = lines.GroupBy(l => l.AccountId).ToDictionary(g => g.Key, g => g.ToList());
            var ids = byAccount.Keys.Union(opening.Where(o => o.Value.Debit != o.Value.Credit).Select(o => o.Key));
            if (accountId != null) ids = new[] { accountId.Value };

            var result = new List<LedgerAccount>();
            foreach (var id in ids)
            {
                if (!accounts.TryGetValue(id, out var acc)) continue;
                var open = opening.TryGetValue(id, out var o) ? Natural(acc.Type, o.Debit, o.Credit) : 0m;
                var running = open;
                var ledgerLines = new List<LedgerLine>();
                foreach (var l in byAccount.GetValueOrDefault(id) ?? new())
                {
                    running += Natural(acc.Type, l.Debit, l.Credit);
                    ledgerLines.Add(new LedgerLine(l.JournalEntryId, l.Date, l.Reference, l.Narration, l.Description, l.Source, l.Debit, l.Credit, running));
                }
                result.Add(new LedgerAccount(acc.Id, acc.Code, acc.Name, acc.Type, open, ledgerLines,
                    ledgerLines.Sum(l => l.Debit), ledgerLines.Sum(l => l.Credit), running));
            }

            return new GeneralLedgerDto(start, end, result.OrderBy(a => a.Code).ToList(), truncated);
        }

        // ------------------------------------------------------------ helpers

        private record AccountBalance(Account Account, decimal Debit, decimal Credit);

        /// <summary>Debit/credit totals per account for journals dated in [from, to] (from = null: since the beginning).</summary>
        private async Task<List<AccountBalance>> Balances(int companyId, DateOnly? from, DateOnly to, int? accountId = null, bool excludeClosing = false)
        {
            var sums = await _db.JournalLines.AsNoTracking()
                .Where(l => l.JournalEntry!.CompanyClientId == companyId && l.JournalEntry.Date <= to
                            && (from == null || l.JournalEntry.Date >= from) && (accountId == null || l.AccountId == accountId)
                            && (!excludeClosing || l.JournalEntry.Source != JournalSource.Closing))
                .GroupBy(l => l.AccountId)
                .Select(g => new { AccountId = g.Key, Debit = g.Sum(l => l.Debit), Credit = g.Sum(l => l.Credit) })
                .ToListAsync();
            var ids = sums.Select(s => s.AccountId).ToList();
            var accounts = await _db.Accounts.AsNoTracking().Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id);
            return sums.Select(s => new AccountBalance(accounts[s.AccountId], s.Debit, s.Credit)).ToList();
        }

        private Task<bool> CompanyExists(int companyId) => _db.CompanyClients.AnyAsync(c => c.Id == companyId);
        private ObjectResult CompanyNotFound() => Problem("La empresa no existe.", statusCode: 404);
    }
}
