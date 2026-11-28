using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    public record MonthSummaryDto(DateOnly From, DateOnly To, decimal Income, decimal Expenses, decimal Net);

    public record DashboardDto(
        List<BankAccountDto> Banks, int UnreconciledCount, decimal BankBalance,
        MonthSummaryDto Month, MonthSummaryDto PreviousMonth,
        DateOnly? LockDate, List<AuditLog> RecentActivity);

    /// <summary>One call for the company overview on the home page.</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public DashboardController(ApplicationDBContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<DashboardDto>> Get(int companyId, [FromQuery] DateOnly? date)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            var today = date ?? DateOnly.FromDateTime(DateTime.Today);

            var bankIds = await _db.BankAccounts.Where(b => b.CompanyClientId == companyId).OrderBy(b => b.Name).Select(b => b.Id).ToListAsync();
            var banks = new List<BankAccountDto>();
            foreach (var id in bankIds) banks.Add((await BankAccountsController.ToDtoAsync(_db, companyId, id))!);

            var monthStart = new DateOnly(today.Year, today.Month, 1);
            var prevStart = monthStart.AddMonths(-1);
            var month = await Summary(companyId, monthStart, monthStart.AddMonths(1).AddDays(-1));
            var prev = await Summary(companyId, prevStart, monthStart.AddDays(-1));

            var lockDate = await Services.AccountingService.LockDateAsync(_db, companyId);
            var recent = await _db.AuditLogs.AsNoTracking().Where(a => a.CompanyClientId == companyId)
                .OrderByDescending(a => a.At).ThenByDescending(a => a.Id).Take(6).ToListAsync();

            return new DashboardDto(banks, banks.Sum(b => b.UnreconciledCount), banks.Sum(b => b.LedgerBalance), month, prev, lockDate, recent);
        }

        private async Task<MonthSummaryDto> Summary(int companyId, DateOnly from, DateOnly to)
        {
            var sums = await _db.JournalLines.AsNoTracking()
                .Where(l => l.JournalEntry!.CompanyClientId == companyId && l.JournalEntry.Date >= from && l.JournalEntry.Date <= to
                            && l.JournalEntry.Source != JournalSource.Closing)
                .GroupBy(l => l.Account!.Type)
                .Select(g => new { Type = g.Key, Net = g.Sum(l => l.Credit - l.Debit) })
                .ToListAsync();
            var income = sums.Where(s => s.Type is AccountType.Revenue or AccountType.OtherIncome).Sum(s => s.Net);
            var expenses = -sums.Where(s => s.Type is AccountType.DirectCosts or AccountType.Expense).Sum(s => s.Net);
            return new MonthSummaryDto(from, to, income, expenses, income - expenses);
        }
    }
}
