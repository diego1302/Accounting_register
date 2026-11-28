using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.DataBase;

using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Services
{
    /// <summary>Thrown for business-rule violations; controllers turn it into a 400/409 ProblemDetails.</summary>
    public class AccountingException : Exception
    {
        public int StatusCode { get; }
        public AccountingException(string message, int statusCode = StatusCodes.Status400BadRequest) : base(message) => StatusCode = statusCode;
    }

    public static class AccountingService
    {
        public const string RetainedEarningsKey = "RETAINED_EARNINGS";

        public static async Task<DateOnly?> LockDateAsync(ApplicationDBContext db, int companyId) =>
            await db.AccountingSettings.Where(s => s.CompanyClientId == companyId).Select(s => s.LockDate).FirstOrDefaultAsync();

        /// <summary>Throws 409 if any of the dates falls in a locked (closed) period.</summary>
        public static async Task EnsureOpenAsync(ApplicationDBContext db, int companyId, params DateOnly[] dates)
        {
            var lockDate = await LockDateAsync(db, companyId);
            if (lockDate is null) return;
            var closed = dates.Where(d => d <= lockDate).OrderBy(d => d).ToList();
            if (closed.Count > 0)
                throw new AccountingException($"El período está cerrado hasta el {lockDate:dd/MM/yyyy} (fecha {closed[0]:dd/MM/yyyy}). Ábrelo en Configuración → Cierre contable.", StatusCodes.Status409Conflict);
        }

        /// <summary>Equity account the year-end closing posts to (code 960 in the standard chart); created if missing.</summary>
        public static async Task<Account> EnsureRetainedEarningsAsync(ApplicationDBContext db, int companyId)
        {
            var acc = await db.Accounts.FirstOrDefaultAsync(a => a.CompanyClientId == companyId && a.SystemKey == RetainedEarningsKey)
                      ?? await db.Accounts.FirstOrDefaultAsync(a => a.CompanyClientId == companyId && a.Code == "960" && a.Type == AccountType.Equity && a.SystemKey == null);
            if (acc != null)
            {
                acc.SystemKey ??= RetainedEarningsKey;
                return acc;
            }
            var codes = await db.Accounts.Where(a => a.CompanyClientId == companyId).Select(a => a.Code).ToListAsync();
            var code = "960";
            for (var n = 960; codes.Contains(code); n++) code = n.ToString();
            acc = new Account { CompanyClientId = companyId, Code = code, Name = "Resultados acumulados", Type = AccountType.Equity, TaxType = TaxType.BASExcluded, SystemKey = RetainedEarningsKey };
            db.Accounts.Add(acc);
            return acc;
        }

        /// <summary>Adds the default chart accounts whose code the company does not use yet. Returns how many were added.</summary>
        public static async Task<int> LoadDefaultChartAsync(ApplicationDBContext db, int companyId)
        {
            var existing = await db.Accounts.Where(a => a.CompanyClientId == companyId).Select(a => a.Code).ToListAsync();
            var keys = await db.Accounts.Where(a => a.CompanyClientId == companyId && a.SystemKey != null).Select(a => a.SystemKey).ToListAsync();
            var added = 0;
            foreach (var (code, name, type, tax, key) in DefaultChartOfAccounts.Accounts)
            {
                if (existing.Contains(code)) continue;
                if (key != null && keys.Contains(key)) continue;
                db.Accounts.Add(new Account { CompanyClientId = companyId, Code = code, Name = name, Type = type, TaxType = tax, SystemKey = key });
                added++;
            }
            await db.SaveChangesAsync();
            return added;
        }

        public static async Task<Account> EnsureGstAccountAsync(ApplicationDBContext db, int companyId)
        {
            var gst = await db.Accounts.FirstOrDefaultAsync(a => a.CompanyClientId == companyId && a.SystemKey == DefaultChartOfAccounts.GstSystemKey);
            if (gst != null) return gst;

            var code = "820";
            var codes = await db.Accounts.Where(a => a.CompanyClientId == companyId).Select(a => a.Code).ToListAsync();
            for (var n = 820; codes.Contains(code); n++) code = n.ToString();
            gst = new Account { CompanyClientId = companyId, Code = code, Name = "GST", Type = AccountType.CurrentLiability, TaxType = TaxType.BASExcluded, SystemKey = DefaultChartOfAccounts.GstSystemKey };
            db.Accounts.Add(gst);
            await db.SaveChangesAsync();
            return gst;
        }

        public static decimal GstPortion(decimal gross, TaxType tax) =>
            tax == TaxType.GST ? Math.Round(gross / 11m, 2, MidpointRounding.AwayFromZero) : 0m;

        /// <summary>
        /// Builds the double-entry journal for a bank line. Money out: Dr expense (net) + Dr GST / Cr bank.
        /// Money in: Dr bank / Cr income (net) + Cr GST.
        /// </summary>
        public static async Task<JournalEntry> BuildBankJournalAsync(ApplicationDBContext db, int companyId, BankTransaction tx, BankAccount bank, ReconcileInput input, string? userId)
        {
            if (input.Lines.Count == 0) throw new AccountingException("Agrega al menos una línea.");
            if (input.Lines.Any(l => l.Amount <= 0)) throw new AccountingException("Los montos de las líneas deben ser mayores que cero.");

            var gross = Math.Abs(tx.Amount);
            var total = input.Lines.Sum(l => Math.Round(l.Amount, 2));
            if (total != gross) throw new AccountingException($"Las líneas suman {total:0.00} pero el movimiento es de {gross:0.00}.");

            var ids = input.Lines.Select(l => l.AccountId).Distinct().ToList();
            var accounts = await db.Accounts.Where(a => a.CompanyClientId == companyId && ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id);
            foreach (var id in ids)
            {
                if (!accounts.TryGetValue(id, out var acc)) throw new AccountingException("Una de las cuentas no existe en esta empresa.");
                if (acc.IsArchived) throw new AccountingException($"La cuenta {acc.Code} {acc.Name} está archivada.");
                if (acc.Id == bank.AccountId) throw new AccountingException("No puedes contabilizar contra la misma cuenta bancaria.");
                if (acc.SystemKey == DefaultChartOfAccounts.GstSystemKey) throw new AccountingException("La cuenta de GST se usa automáticamente; elige la cuenta de gasto o ingreso.");
            }

            var moneyIn = tx.Amount > 0;
            var entry = new JournalEntry
            {
                CompanyClientId = companyId,
                Date = tx.Date,
                Reference = string.IsNullOrWhiteSpace(input.Reference) ? tx.Reference : input.Reference.Trim(),
                Narration = string.IsNullOrWhiteSpace(input.Narration) ? tx.Description : input.Narration.Trim(),
                Source = JournalSource.Bank,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
            };

            var gstTotal = 0m;
            foreach (var l in input.Lines)
            {
                var amount = Math.Round(l.Amount, 2);
                var gst = GstPortion(amount, l.TaxType);
                gstTotal += gst;
                var net = amount - gst;
                entry.Lines.Add(new JournalLine
                {
                    AccountId = l.AccountId,
                    Description = string.IsNullOrWhiteSpace(l.Description) ? null : l.Description.Trim(),
                    Debit = moneyIn ? 0 : net,
                    Credit = moneyIn ? net : 0,
                    TaxType = l.TaxType,
                });
            }

            if (gstTotal != 0)
            {
                var gstAccount = await EnsureGstAccountAsync(db, companyId);
                entry.Lines.Add(new JournalLine
                {
                    AccountId = gstAccount.Id,
                    Description = "GST",
                    Debit = moneyIn ? 0 : gstTotal,
                    Credit = moneyIn ? gstTotal : 0,
                });
            }

            entry.Lines.Add(new JournalLine
            {
                AccountId = bank.AccountId,
                Description = tx.Description,
                Debit = moneyIn ? gross : 0,
                Credit = moneyIn ? 0 : gross,
            });

            if (entry.Lines.Sum(x => x.Debit) != entry.Lines.Sum(x => x.Credit))
                throw new AccountingException("El asiento no cuadra (debe ≠ haber).");

            return entry;
        }
    }
}
