using System.Security.Claims;

using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.DataBase;

using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Services
{
    /// <summary>Reconciling bank lines: by coding, as a transfer between own accounts, or by matching an existing journal.</summary>
    public static class ReconciliationService
    {
        /// <summary>Days either side of the bank date within which matches and transfer counterparts are proposed.</summary>
        public const int MatchWindowDays = 5;

        /// <summary>Codes one bank line and posts its journal. Throws <see cref="AccountingException"/> on business-rule errors.</summary>
        public static async Task<JournalEntry> ReconcileAsync(ApplicationDBContext db, int companyId, BankTransaction tx, ReconcileInput input,
            ClaimsPrincipal? user, string? actor = null, string auditAction = "Reconciled")
        {
            if (tx.Status == BankTransactionStatus.Reconciled) throw new AccountingException("Este movimiento ya está conciliado.", StatusCodes.Status409Conflict);
            await AccountingService.EnsureOpenAsync(db, companyId, tx.Date);
            var bank = tx.BankAccount ?? await db.BankAccounts.FirstAsync(b => b.Id == tx.BankAccountId);

            using var trx = await db.Database.BeginTransactionAsync();
            var entry = await AccountingService.BuildBankJournalAsync(db, companyId, tx, bank, input, user?.FindFirstValue(ClaimTypes.NameIdentifier));
            entry.CreatedBy = actor ?? Audit.ActorName(user);
            db.JournalEntries.Add(entry);
            await db.SaveChangesAsync();
            await LinkAsync(db, tx, entry.Id);
            Audit.Log(db, user, companyId, auditAction, "BankTransaction", tx.Id,
                $"{(auditAction == "AutoReconciled" ? "Conciliado por regla" : "Conciliado")}: {tx.Description} ({tx.Amount:0.00}) → asiento #{entry.Id}", actor: actor);
            await db.SaveChangesAsync();
            await trx.CommitAsync();
            return entry;
        }

        /// <summary>Money moved between two bank accounts of the company: Dr receiving bank / Cr paying bank, never income or expense.</summary>
        public static async Task<JournalEntry> TransferAsync(ApplicationDBContext db, int companyId, BankTransaction tx, TransferInput input, ClaimsPrincipal? user)
        {
            if (tx.Status == BankTransactionStatus.Reconciled) throw new AccountingException("Este movimiento ya está conciliado.", StatusCodes.Status409Conflict);
            var bank = tx.BankAccount ?? await db.BankAccounts.FirstAsync(b => b.Id == tx.BankAccountId);
            if (input.ToBankAccountId == bank.Id) throw new AccountingException("Elige la otra cuenta bancaria de la transferencia.");
            var other = await db.BankAccounts.FirstOrDefaultAsync(b => b.Id == input.ToBankAccountId && b.CompanyClientId == companyId)
                        ?? throw new AccountingException("La cuenta bancaria de destino no existe en esta empresa.");

            BankTransaction? counterpart = null;
            if (input.CounterpartTransactionId != null)
            {
                counterpart = await db.BankTransactions.FirstOrDefaultAsync(t => t.Id == input.CounterpartTransactionId && t.BankAccountId == other.Id)
                              ?? throw new AccountingException("El movimiento de la otra cuenta no existe.");
                if (counterpart.Status == BankTransactionStatus.Reconciled) throw new AccountingException("El movimiento de la otra cuenta ya está conciliado.", StatusCodes.Status409Conflict);
                if (counterpart.Amount != -tx.Amount) throw new AccountingException($"Los montos no coinciden ({tx.Amount:0.00} / {counterpart.Amount:0.00}).");
            }
            await AccountingService.EnsureOpenAsync(db, companyId, counterpart == null ? new[] { tx.Date } : new[] { tx.Date, counterpart.Date });

            var amount = Math.Abs(tx.Amount);
            var moneyIn = tx.Amount > 0;
            var narration = string.IsNullOrWhiteSpace(input.Narration)
                ? (moneyIn ? $"Transferencia desde {other.Name}" : $"Transferencia a {other.Name}")
                : input.Narration.Trim();
            var entry = new JournalEntry
            {
                CompanyClientId = companyId,
                Date = tx.Date,
                Reference = tx.Reference,
                Narration = narration.Length > 500 ? narration[..500] : narration,
                Source = JournalSource.Bank,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user?.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedBy = Audit.ActorName(user),
                Lines =
                {
                    new JournalLine { AccountId = moneyIn ? bank.AccountId : other.AccountId, Description = tx.Description, Debit = amount },
                    new JournalLine { AccountId = moneyIn ? other.AccountId : bank.AccountId, Description = counterpart?.Description ?? tx.Description, Credit = amount },
                },
            };

            using var trx = await db.Database.BeginTransactionAsync();
            db.JournalEntries.Add(entry);
            await db.SaveChangesAsync();
            await LinkAsync(db, tx, entry.Id);
            if (counterpart != null) await LinkAsync(db, counterpart, entry.Id);
            Audit.Log(db, user, companyId, "TransferRecorded", "BankTransaction", tx.Id,
                $"Transferencia {bank.Name} ↔ {other.Name} ({amount:0.00}) → asiento #{entry.Id}{(counterpart != null ? " (ambos lados conciliados)" : "")}");
            await db.SaveChangesAsync();
            await trx.CommitAsync();
            return entry;
        }

        /// <summary>Links the bank line to a journal that already records it (no new posting).</summary>
        public static async Task<JournalEntry> MatchAsync(ApplicationDBContext db, int companyId, BankTransaction tx, int journalEntryId, ClaimsPrincipal? user)
        {
            if (tx.Status == BankTransactionStatus.Reconciled) throw new AccountingException("Este movimiento ya está conciliado.", StatusCodes.Status409Conflict);
            await AccountingService.EnsureOpenAsync(db, companyId, tx.Date);
            var bank = tx.BankAccount ?? await db.BankAccounts.FirstAsync(b => b.Id == tx.BankAccountId);
            var entry = await db.JournalEntries.Include(e => e.Lines).FirstOrDefaultAsync(e => e.Id == journalEntryId && e.CompanyClientId == companyId)
                        ?? throw new AccountingException("El asiento no existe en esta empresa.");
            if (entry.Source == JournalSource.Closing) throw new AccountingException("No se puede conciliar contra un asiento de cierre.");
            var amount = Math.Abs(tx.Amount);
            var fits = entry.Lines.Any(l => l.AccountId == bank.AccountId && (tx.Amount > 0 ? l.Debit == amount : l.Credit == amount));
            if (!fits) throw new AccountingException($"El asiento #{entry.Id} no tiene un {(tx.Amount > 0 ? "débito" : "crédito")} de {amount:0.00} en la cuenta {bank.Name}.");
            if (await db.BankTransactions.AnyAsync(t => t.BankAccountId == bank.Id && t.JournalEntryId == entry.Id))
                throw new AccountingException($"El asiento #{entry.Id} ya está conciliado con otro movimiento de esta cuenta.", StatusCodes.Status409Conflict);

            using var trx = await db.Database.BeginTransactionAsync();
            await LinkAsync(db, tx, entry.Id);
            Audit.Log(db, user, companyId, "Matched", "BankTransaction", tx.Id, $"Conciliado con asiento existente #{entry.Id}: {tx.Description} ({tx.Amount:0.00})");
            await db.SaveChangesAsync();
            await trx.CommitAsync();
            return entry;
        }

        /// <summary>
        /// Undo: the line goes back to pending. The journal is deleted only if it came from bank reconciliation and no
        /// other bank line still uses it (e.g. the other side of a transfer); matched manual/external journals stay.
        /// </summary>
        public static async Task UnreconcileAsync(ApplicationDBContext db, int companyId, BankTransaction tx, ClaimsPrincipal? user)
        {
            if (tx.Status != BankTransactionStatus.Reconciled || tx.JournalEntryId is null)
                throw new AccountingException("Este movimiento no está conciliado.", StatusCodes.Status409Conflict);
            await AccountingService.EnsureOpenAsync(db, companyId, tx.Date);
            var entryId = tx.JournalEntryId.Value;
            var entry = await db.JournalEntries.Include(e => e.Lines).ThenInclude(l => l.Account).FirstAsync(e => e.Id == entryId);
            var othersLinked = await db.BankTransactions.AnyAsync(t => t.JournalEntryId == entryId && t.Id != tx.Id);
            var deleteJournal = entry.Source == JournalSource.Bank && !othersLinked;
            if (deleteJournal) await AccountingService.EnsureOpenAsync(db, companyId, entry.Date);

            using var trx = await db.Database.BeginTransactionAsync();
            tx.JournalEntryId = null;
            tx.Status = BankTransactionStatus.Unreconciled;
            Audit.Log(db, user, companyId, "Unreconciled", "BankTransaction", tx.Id,
                $"Conciliación deshecha: {tx.Description} ({tx.Amount:0.00}){(deleteJournal ? $"; asiento #{entryId} eliminado" : $"; asiento #{entryId} se mantiene")}",
                deleteJournal ? Audit.Snapshot(entry) : null);
            await db.SaveChangesAsync();
            if (deleteJournal)
            {
                await db.JournalLines.Where(l => l.JournalEntryId == entryId).ExecuteDeleteAsync();
                await db.JournalEntries.Where(e => e.Id == entryId).ExecuteDeleteAsync();
            }
            await trx.CommitAsync();
        }

        /// <summary>Applies auto-reconcile rules to the given pending lines (all pending of the account when null). Returns how many were reconciled.</summary>
        public static async Task<int> AutoReconcileAsync(ApplicationDBContext db, int companyId, int bankAccountId, ICollection<int>? transactionIds, ClaimsPrincipal? user)
        {
            var rules = await ActiveRulesAsync(db, companyId, bankAccountId, autoOnly: true);
            if (rules.Count == 0) return 0;
            var query = db.BankTransactions.Include(t => t.BankAccount)
                .Where(t => t.BankAccountId == bankAccountId && t.Status == BankTransactionStatus.Unreconciled);
            if (transactionIds != null) query = query.Where(t => transactionIds.Contains(t.Id));
            var pending = await query.OrderBy(t => t.Date).ThenBy(t => t.Id).ToListAsync();
            var lockDate = await AccountingService.LockDateAsync(db, companyId);

            var count = 0;
            foreach (var tx in pending)
            {
                if (lockDate != null && tx.Date <= lockDate) continue;
                var rule = rules.FirstOrDefault(r => r.Matches(tx));
                if (rule is null) continue;
                try
                {
                    await ReconcileAsync(db, companyId, tx, new ReconcileInput
                    {
                        Lines = { new ReconcileLineInput { AccountId = rule.AccountId, Amount = Math.Abs(tx.Amount), TaxType = rule.TaxType, Description = rule.LineDescription } },
                    }, user, actor: $"Regla: {rule.Name}", auditAction: "AutoReconciled");
                    count++;
                }
                catch (AccountingException)
                {
                    db.ChangeTracker.Clear(); // e.g. the rule's account was archived: leave the line pending
                }
            }
            return count;
        }

        public static Task<List<BankRule>> ActiveRulesAsync(ApplicationDBContext db, int companyId, int bankAccountId, bool autoOnly = false) =>
            db.BankRules.AsNoTracking()
                .Where(r => r.CompanyClientId == companyId && r.IsActive && (r.BankAccountId == null || r.BankAccountId == bankAccountId) && (!autoOnly || r.AutoReconcile))
                .OrderBy(r => r.Priority).ThenBy(r => r.Id).ToListAsync();

        /// <summary>Conditional update so two people reconciling the same line at once cannot both succeed.</summary>
        private static async Task LinkAsync(ApplicationDBContext db, BankTransaction tx, int entryId)
        {
            var updated = await db.BankTransactions
                .Where(t => t.Id == tx.Id && t.Status == BankTransactionStatus.Unreconciled)
                .ExecuteUpdateAsync(u => u.SetProperty(t => t.Status, BankTransactionStatus.Reconciled).SetProperty(t => t.JournalEntryId, entryId));
            if (updated == 0) throw new AccountingException("El movimiento cambió mientras lo conciliabas. Recarga e inténtalo de nuevo.", StatusCodes.Status409Conflict);
            tx.Status = BankTransactionStatus.Reconciled;
            tx.JournalEntryId = entryId;
            db.Entry(tx).State = EntityState.Unchanged; // already persisted by the bulk update
        }
    }
}
