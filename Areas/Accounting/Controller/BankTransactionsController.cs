using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.Areas.Accounting.Services;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    /// <summary>Reconciling bank lines: coding (posts a journal), transfer between own accounts, match to an existing journal, bulk, undo.</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/bank-transactions")]
    public class BankTransactionsController : ControllerBase
    {
        public const int MaxBulk = 200;
        private readonly ApplicationDBContext _db;

        public BankTransactionsController(ApplicationDBContext db) => _db = db;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BankTransactionDto>> Get(int companyId, int id)
        {
            var tx = await Find(companyId, id, tracking: false);
            return tx is null ? NotFound() : await Dto(companyId, tx);
        }

        [HttpPost("{id:int}/reconcile")]
        public Task<ActionResult<BankTransactionDto>> Reconcile(int companyId, int id, ReconcileInput input) =>
            Run(companyId, id, tx => ReconciliationService.ReconcileAsync(_db, companyId, tx, input, User));

        /// <summary>Transfer to/from another bank account of the company. With CounterpartTransactionId both lines are reconciled with one journal.</summary>
        [HttpPost("{id:int}/transfer")]
        public Task<ActionResult<BankTransactionDto>> Transfer(int companyId, int id, TransferInput input) =>
            Run(companyId, id, tx => ReconciliationService.TransferAsync(_db, companyId, tx, input, User));

        /// <summary>Links the line to a journal that already records it (manual, external, or the other side of a transfer).</summary>
        [HttpPost("{id:int}/match")]
        public Task<ActionResult<BankTransactionDto>> Match(int companyId, int id, MatchInput input) =>
            Run(companyId, id, tx => ReconciliationService.MatchAsync(_db, companyId, tx, input.JournalEntryId, User));

        /// <summary>Undo: the line goes back to pending (see <see cref="ReconciliationService.UnreconcileAsync"/>).</summary>
        [HttpDelete("{id:int}/reconcile")]
        public async Task<ActionResult<BankTransactionDto>> Unreconcile(int companyId, int id)
        {
            var tx = await Find(companyId, id, tracking: true);
            if (tx is null) return NotFound();
            try
            {
                await ReconciliationService.UnreconcileAsync(_db, companyId, tx, User);
            }
            catch (AccountingException ex)
            {
                return Problem(ex.Message, statusCode: ex.StatusCode);
            }
            return await Dto(companyId, tx);
        }

        /// <summary>Reconciles many lines with one account each. Every line is independent: the result says which ones failed and why.</summary>
        [HttpPost("reconcile-bulk")]
        public async Task<ActionResult<List<BulkReconcileResult>>> Bulk(int companyId, BulkReconcileInput input)
        {
            if (input.Items.Count == 0) return Problem("No hay movimientos seleccionados.", statusCode: 400);
            if (input.Items.Count > MaxBulk) return Problem($"Máximo {MaxBulk} movimientos por vez.", statusCode: 400);
            var results = new List<BulkReconcileResult>();
            foreach (var item in input.Items)
            {
                var tx = await Find(companyId, item.TransactionId, tracking: true);
                if (tx is null) { results.Add(new(item.TransactionId, false, "El movimiento no existe.")); continue; }
                try
                {
                    await ReconciliationService.ReconcileAsync(_db, companyId, tx, new ReconcileInput
                    {
                        Lines = { new ReconcileLineInput { AccountId = item.AccountId, Amount = Math.Abs(tx.Amount), TaxType = item.TaxType, Description = item.Description } },
                    }, User);
                    results.Add(new(item.TransactionId, true, null));
                }
                catch (AccountingException ex)
                {
                    _db.ChangeTracker.Clear();
                    results.Add(new(item.TransactionId, false, ex.Message));
                }
            }
            return results;
        }

        private async Task<ActionResult<BankTransactionDto>> Run(int companyId, int id, Func<BankTransaction, Task<JournalEntry>> action)
        {
            var tx = await Find(companyId, id, tracking: true);
            if (tx is null) return NotFound();
            try
            {
                await action(tx);
            }
            catch (AccountingException ex)
            {
                return Problem(ex.Message, statusCode: ex.StatusCode);
            }
            _db.ChangeTracker.Clear();
            var fresh = await Find(companyId, id, tracking: false);
            return await Dto(companyId, fresh!);
        }

        private Task<BankTransaction?> Find(int companyId, int id, bool tracking)
        {
            var q = _db.BankTransactions.Include(t => t.BankAccount).Where(t => t.Id == id && t.BankAccount!.CompanyClientId == companyId);
            return (tracking ? q : q.AsNoTracking()).FirstOrDefaultAsync();
        }

        private async Task<BankTransactionDto> Dto(int companyId, BankTransaction tx) =>
            (await BankTransactionMapper.ToDtos(_db, companyId, tx.BankAccount!, new() { tx }))[0];
    }

    internal static class BankTransactionMapper
    {
        /// <summary>
        /// Adds to each line: the posted journal (reconciled), and for pending lines a coding suggestion (bank rule first,
        /// else the last identical reconciled line), existing journals that could match it, and transfer counterparts.
        /// </summary>
        public static async Task<List<BankTransactionDto>> ToDtos(ApplicationDBContext db, int companyId, BankAccount bank, List<BankTransaction> items)
        {
            var entryIds = items.Where(t => t.JournalEntryId != null).Select(t => t.JournalEntryId!.Value).Distinct().ToList();
            var entries = entryIds.Count == 0
                ? new Dictionary<int, JournalEntryDto>()
                : (await db.JournalEntries.AsNoTracking().Where(e => entryIds.Contains(e.Id))
                    .Include(e => e.Lines).ThenInclude(l => l.Account).ToListAsync())
                    .ToDictionary(e => e.Id, e => new JournalEntryDto(e.Id, e.Date, e.Reference, e.Narration, e.Source,
                        e.Lines.OrderByDescending(l => l.AccountId != bank.AccountId).ThenBy(l => l.Id)
                            .Select(l => new JournalLineDto(l.AccountId, l.Account!.Code, l.Account.Name, l.Description, l.Debit, l.Credit, l.TaxType)).ToList(),
                        e.CreatedBy));

            var pending = items.Where(t => t.Status == BankTransactionStatus.Unreconciled).ToList();
            var suggestions = new Dictionary<int, SuggestionDto>();
            var matches = new Dictionary<int, List<MatchCandidateDto>>();
            var transfers = new Dictionary<int, List<TransferCandidateDto>>();

            if (pending.Count > 0)
            {
                // 1) Bank rules
                var rules = await ReconciliationService.ActiveRulesAsync(db, companyId, bank.Id);
                var archived = await db.Accounts.Where(a => a.CompanyClientId == companyId && a.IsArchived).Select(a => a.Id).ToListAsync();
                foreach (var t in pending)
                {
                    var rule = rules.FirstOrDefault(r => r.Matches(t) && !archived.Contains(r.AccountId));
                    if (rule != null) suggestions[t.Id] = new SuggestionDto(rule.AccountId, rule.TaxType, rule.Id, rule.Name, rule.LineDescription);
                }

                // 2) History: last reconciliation of an identical description (single-account postings only)
                var descriptions = pending.Where(t => !suggestions.ContainsKey(t.Id)).Select(t => t.Description).Distinct().ToList();
                if (descriptions.Count > 0)
                {
                    var history = await db.BankTransactions.AsNoTracking()
                        .Where(t => t.BankAccountId == bank.Id && t.Status == BankTransactionStatus.Reconciled && descriptions.Contains(t.Description))
                        .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
                        .Select(t => new
                        {
                            t.Description,
                            Lines = t.JournalEntry!.Lines
                                .Where(l => l.AccountId != bank.AccountId && l.Account!.SystemKey == null)
                                .Select(l => new { l.AccountId, l.TaxType, Archived = l.Account!.IsArchived, IsBank = l.Account.Type == AccountType.Bank }).ToList(),
                        })
                        .Take(500).ToListAsync();
                    var byDesc = new Dictionary<string, SuggestionDto>();
                    foreach (var h in history)
                    {
                        if (byDesc.ContainsKey(h.Description) || h.Lines.Count != 1 || h.Lines[0].Archived || h.Lines[0].IsBank) continue;
                        byDesc[h.Description] = new SuggestionDto(h.Lines[0].AccountId, h.Lines[0].TaxType ?? TaxType.BASExcluded);
                    }
                    foreach (var t in pending.Where(t => !suggestions.ContainsKey(t.Id)))
                        if (byDesc.TryGetValue(t.Description, out var s)) suggestions[t.Id] = s;
                }

                var from = pending.Min(t => t.Date).AddDays(-ReconciliationService.MatchWindowDays);
                var to = pending.Max(t => t.Date).AddDays(ReconciliationService.MatchWindowDays);

                // 3) Existing journals hitting this bank's ledger account not yet linked to a line of this account
                var candidates = await db.JournalLines.AsNoTracking()
                    .Where(l => l.AccountId == bank.AccountId && l.JournalEntry!.Date >= from && l.JournalEntry.Date <= to
                                && l.JournalEntry.Source != JournalSource.Closing
                                && !db.BankTransactions.Any(t => t.BankAccountId == bank.Id && t.JournalEntryId == l.JournalEntryId))
                    .Select(l => new { l.JournalEntryId, l.JournalEntry!.Date, l.JournalEntry.Narration, l.JournalEntry.Reference, l.JournalEntry.Source, l.Debit, l.Credit })
                    .Take(500).ToListAsync();
                foreach (var t in pending)
                {
                    var abs = Math.Abs(t.Amount);
                    var list = candidates
                        .Where(c => (t.Amount > 0 ? c.Debit == abs : c.Credit == abs) && Math.Abs(c.Date.DayNumber - t.Date.DayNumber) <= ReconciliationService.MatchWindowDays)
                        .OrderBy(c => Math.Abs(c.Date.DayNumber - t.Date.DayNumber))
                        .Select(c => new MatchCandidateDto(c.JournalEntryId, c.Date, c.Narration, c.Reference, c.Source))
                        .DistinctBy(c => c.JournalEntryId).Take(3).ToList();
                    if (list.Count > 0) matches[t.Id] = list;
                }

                // 4) Pending lines in the company's other bank accounts with the opposite amount
                var amounts = pending.Select(t => -t.Amount).Distinct().ToList();
                var others = await db.BankTransactions.AsNoTracking()
                    .Where(t => t.BankAccount!.CompanyClientId == companyId && t.BankAccountId != bank.Id && t.Status == BankTransactionStatus.Unreconciled
                                && t.Date >= from && t.Date <= to && amounts.Contains(t.Amount))
                    .Select(t => new { t.Id, t.BankAccountId, BankName = t.BankAccount!.Name, t.Date, t.Description, t.Amount })
                    .Take(500).ToListAsync();
                foreach (var t in pending)
                {
                    var list = others
                        .Where(o => o.Amount == -t.Amount && Math.Abs(o.Date.DayNumber - t.Date.DayNumber) <= ReconciliationService.MatchWindowDays)
                        .OrderBy(o => Math.Abs(o.Date.DayNumber - t.Date.DayNumber))
                        .Select(o => new TransferCandidateDto(o.Id, o.BankAccountId, o.BankName, o.Date, o.Description, o.Amount)).Take(3).ToList();
                    if (list.Count > 0) transfers[t.Id] = list;
                }
            }

            return items.Select(t => new BankTransactionDto(
                t.Id, t.BankAccountId, t.BankStatementId, t.Date, t.Description, t.Reference, t.Amount, t.Balance, t.Status,
                t.JournalEntryId is int je && entries.TryGetValue(je, out var e) ? e : null,
                suggestions.GetValueOrDefault(t.Id),
                matches.GetValueOrDefault(t.Id) ?? new(),
                transfers.GetValueOrDefault(t.Id) ?? new())).ToList();
        }
    }
}
