using System.Security.Claims;

using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.Areas.Accounting.Services;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    /// <summary>Reconciling a bank line = posting its double-entry journal.</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/bank-transactions")]
    public class BankTransactionsController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public BankTransactionsController(ApplicationDBContext db) => _db = db;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BankTransactionDto>> Get(int companyId, int id)
        {
            var tx = await _db.BankTransactions.AsNoTracking().Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.Id == id && t.BankAccount!.CompanyClientId == companyId);
            if (tx is null) return NotFound();
            return (await BankTransactionMapper.ToDtos(_db, companyId, tx.BankAccount!, new() { tx }))[0];
        }

        [HttpPost("{id:int}/reconcile")]
        public async Task<ActionResult<BankTransactionDto>> Reconcile(int companyId, int id, ReconcileInput input)
        {
            var tx = await _db.BankTransactions.Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.Id == id && t.BankAccount!.CompanyClientId == companyId);
            if (tx is null) return NotFound();
            if (tx.Status == BankTransactionStatus.Reconciled) return Problem("Este movimiento ya está conciliado.", statusCode: 409);

            try
            {
                using var trx = await _db.Database.BeginTransactionAsync();
                var entry = await AccountingService.BuildBankJournalAsync(_db, companyId, tx, tx.BankAccount!, input, User.FindFirstValue(ClaimTypes.NameIdentifier));
                _db.JournalEntries.Add(entry);
                await _db.SaveChangesAsync();
                // Conditional update so two people reconciling the same line at once cannot both post a journal.
                var updated = await _db.BankTransactions
                    .Where(t => t.Id == id && t.Status == BankTransactionStatus.Unreconciled)
                    .ExecuteUpdateAsync(u => u.SetProperty(t => t.Status, BankTransactionStatus.Reconciled).SetProperty(t => t.JournalEntryId, entry.Id));
                if (updated == 0) throw new DbUpdateConcurrencyException();
                await trx.CommitAsync();
                tx.JournalEntryId = entry.Id;
                tx.Status = BankTransactionStatus.Reconciled;
            }
            catch (AccountingException ex)
            {
                return Problem(ex.Message, statusCode: ex.StatusCode);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Problem("El movimiento cambió mientras lo conciliabas. Recarga e inténtalo de nuevo.", statusCode: 409);
            }

            return (await BankTransactionMapper.ToDtos(_db, companyId, tx.BankAccount!, new() { tx }))[0];
        }

        /// <summary>Undo: deletes the journal and returns the line to "pending".</summary>
        [HttpDelete("{id:int}/reconcile")]
        public async Task<ActionResult<BankTransactionDto>> Unreconcile(int companyId, int id)
        {
            var tx = await _db.BankTransactions.Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.Id == id && t.BankAccount!.CompanyClientId == companyId);
            if (tx is null) return NotFound();
            if (tx.Status != BankTransactionStatus.Reconciled || tx.JournalEntryId is null) return Problem("Este movimiento no está conciliado.", statusCode: 409);

            using var trx = await _db.Database.BeginTransactionAsync();
            var entryId = tx.JournalEntryId.Value;
            tx.JournalEntryId = null;
            tx.Status = BankTransactionStatus.Unreconciled;
            await _db.SaveChangesAsync();
            await _db.JournalLines.Where(l => l.JournalEntryId == entryId).ExecuteDeleteAsync();
            await _db.JournalEntries.Where(e => e.Id == entryId).ExecuteDeleteAsync();
            await trx.CommitAsync();

            return (await BankTransactionMapper.ToDtos(_db, companyId, tx.BankAccount!, new() { tx }))[0];
        }
    }

    internal static class BankTransactionMapper
    {
        /// <summary>
        /// Adds the posted journal to reconciled lines, and to pending lines a suggestion taken from the last time a
        /// line with the same description was reconciled in this bank account (single-account postings only).
        /// </summary>
        public static async Task<List<BankTransactionDto>> ToDtos(ApplicationDBContext db, int companyId, BankAccount bank, List<BankTransaction> items)
        {
            var entryIds = items.Where(t => t.JournalEntryId != null).Select(t => t.JournalEntryId!.Value).ToList();
            var entries = entryIds.Count == 0
                ? new Dictionary<int, JournalEntryDto>()
                : (await db.JournalEntries.AsNoTracking().Where(e => entryIds.Contains(e.Id))
                    .Include(e => e.Lines).ThenInclude(l => l.Account).ToListAsync())
                    .ToDictionary(e => e.Id, e => new JournalEntryDto(e.Id, e.Date, e.Reference, e.Narration, e.Source,
                        e.Lines.OrderByDescending(l => l.AccountId != bank.AccountId).ThenBy(l => l.Id)
                            .Select(l => new JournalLineDto(l.AccountId, l.Account!.Code, l.Account.Name, l.Description, l.Debit, l.Credit, l.TaxType)).ToList()));

            var pendingDescriptions = items.Where(t => t.Status == BankTransactionStatus.Unreconciled).Select(t => t.Description).Distinct().ToList();
            var suggestions = new Dictionary<string, SuggestionDto>();
            if (pendingDescriptions.Count > 0)
            {
                var history = await db.BankTransactions.AsNoTracking()
                    .Where(t => t.BankAccountId == bank.Id && t.Status == BankTransactionStatus.Reconciled && pendingDescriptions.Contains(t.Description))
                    .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
                    .Select(t => new
                    {
                        t.Description,
                        Lines = t.JournalEntry!.Lines
                            .Where(l => l.AccountId != bank.AccountId && l.Account!.SystemKey == null)
                            .Select(l => new { l.AccountId, l.TaxType, Archived = l.Account!.IsArchived }).ToList(),
                    })
                    .Take(500).ToListAsync();
                foreach (var h in history)
                {
                    if (suggestions.ContainsKey(h.Description) || h.Lines.Count != 1 || h.Lines[0].Archived) continue;
                    suggestions[h.Description] = new SuggestionDto(h.Lines[0].AccountId, h.Lines[0].TaxType ?? TaxType.BASExcluded);
                }
            }

            return items.Select(t => new BankTransactionDto(
                t.Id, t.BankAccountId, t.BankStatementId, t.Date, t.Description, t.Reference, t.Amount, t.Balance, t.Status,
                t.JournalEntryId is int je && entries.TryGetValue(je, out var e) ? e : null,
                t.Status == BankTransactionStatus.Unreconciled && suggestions.TryGetValue(t.Description, out var s) ? s : null)).ToList();
        }
    }
}
