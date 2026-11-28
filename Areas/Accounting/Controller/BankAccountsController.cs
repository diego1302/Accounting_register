using System.Security.Claims;
using System.Text.Json;

using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.Areas.Accounting.Services;
using Accounting_Register_BE.DataBase;
using Accounting_Register_BE.Resources.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    /// <summary>Bank accounts of a company client, their statement imports and their transactions.</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/bank-accounts")]
    public class BankAccountsController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
        private readonly ApplicationDBContext _db;

        public BankAccountsController(ApplicationDBContext db) => _db = db;

        // ------------------------------------------------------------ bank accounts

        [HttpGet]
        public async Task<ActionResult<List<BankAccountDto>>> List(int companyId)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            var banks = await _db.BankAccounts.AsNoTracking().Where(b => b.CompanyClientId == companyId).OrderBy(b => b.Name).Select(b => b.Id).ToListAsync();
            var result = new List<BankAccountDto>();
            foreach (var id in banks) result.Add((await ToDto(companyId, id))!);
            return result;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BankAccountDto>> Get(int companyId, int id)
        {
            var dto = await ToDto(companyId, id);
            return dto is null ? NotFound() : dto;
        }

        [HttpPost]
        public async Task<ActionResult<BankAccountDto>> Create(int companyId, BankAccountInput input)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);

            // A company starting with banks gets the standard chart so there is something to reconcile against.
            if (!await _db.Accounts.AnyAsync(a => a.CompanyClientId == companyId))
                await AccountingService.LoadDefaultChartAsync(_db, companyId);

            var codes = await _db.Accounts.Where(a => a.CompanyClientId == companyId).Select(a => a.Code).ToListAsync();
            var code = input.AccountCode?.Trim();
            if (string.IsNullOrEmpty(code))
            {
                var n = 90;
                while (codes.Contains(n.ToString("000"))) n++;
                code = n.ToString("000");
            }
            else if (codes.Contains(code)) return Problem($"Ya existe una cuenta con el código {code}.", statusCode: 409);

            using var trx = await _db.Database.BeginTransactionAsync();
            var account = new Account { CompanyClientId = companyId, Code = code, Name = input.Name.Trim(), Type = AccountType.Bank, TaxType = TaxType.BASExcluded };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var bank = new BankAccount
            {
                CompanyClientId = companyId,
                AccountId = account.Id,
                Name = input.Name.Trim(),
                BankName = Clean(input.BankName),
                BSB = Clean(input.BSB),
                AccountNumber = Clean(input.AccountNumber),
                Currency = string.IsNullOrWhiteSpace(input.Currency) ? "AUD" : input.Currency.Trim().ToUpperInvariant(),
            };
            _db.BankAccounts.Add(bank);
            await _db.SaveChangesAsync();
            Audit.Log(_db, User, companyId, "BankAccountCreated", "BankAccount", bank.Id, $"Cuenta bancaria creada: {bank.Name} (cuenta contable {code})");
            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            return CreatedAtAction(nameof(Get), new { companyId, id = bank.Id }, await ToDto(companyId, bank.Id));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<BankAccountDto>> Update(int companyId, int id, BankAccountInput input)
        {
            var bank = await _db.BankAccounts.Include(b => b.Account).FirstOrDefaultAsync(b => b.CompanyClientId == companyId && b.Id == id);
            if (bank is null) return NotFound();
            bank.Name = input.Name.Trim();
            bank.BankName = Clean(input.BankName);
            bank.BSB = Clean(input.BSB);
            bank.AccountNumber = Clean(input.AccountNumber);
            if (!string.IsNullOrWhiteSpace(input.Currency)) bank.Currency = input.Currency.Trim().ToUpperInvariant();
            bank.Account!.Name = bank.Name;
            await _db.SaveChangesAsync();
            return (await ToDto(companyId, id))!;
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int companyId, int id)
        {
            var bank = await _db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(b => b.CompanyClientId == companyId && b.Id == id);
            if (bank is null) return NotFound();
            if (await _db.BankTransactions.AnyAsync(t => t.BankAccountId == id && t.Status == BankTransactionStatus.Reconciled))
                return Problem("La cuenta tiene movimientos conciliados. Deshaz la conciliación antes de eliminarla.", statusCode: 409);
            if (await _db.JournalLines.AnyAsync(l => l.AccountId == bank.AccountId))
                return Problem("La cuenta contable del banco tiene asientos asociados.", statusCode: 409);

            using var trx = await _db.Database.BeginTransactionAsync();
            await _db.BankTransactions.Where(t => t.BankAccountId == id).ExecuteDeleteAsync();
            await _db.BankStatements.Where(s => s.BankAccountId == id).ExecuteDeleteAsync();
            var accountId = bank.AccountId;
            await _db.BankRules.Where(r => r.BankAccountId == id).ExecuteDeleteAsync();
            await _db.BankAccounts.Where(b => b.Id == id).ExecuteDeleteAsync();
            await _db.Accounts.Where(a => a.Id == accountId).ExecuteDeleteAsync();
            Audit.Log(_db, User, companyId, "BankAccountDeleted", "BankAccount", id, $"Cuenta bancaria eliminada: {bank.Name}");
            await _db.SaveChangesAsync();
            await trx.CommitAsync();
            return NoContent();
        }

        // ------------------------------------------------------------ statements

        /// <summary>Reads an uploaded file and returns the detected format, the first rows and a suggested column mapping.</summary>
        [HttpPost("{id:int}/statements/preview")]
        [RequestSizeLimit(StatementParser.MaxFileBytes + 64 * 1024)]
        public async Task<ActionResult<StatementPreview>> Preview(int companyId, int id, IFormFile file, [FromForm] string? mapping)
        {
            var bank = await _db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(b => b.CompanyClientId == companyId && b.Id == id);
            if (bank is null) return NotFound();
            var (content, error) = await ReadFile(file);
            if (error != null) return error;

            var format = StatementParser.DetectFormat(file.FileName, content!);
            try
            {
                // An explicit mapping (user is adjusting columns) wins; otherwise the one remembered from the last import.
                if (!string.IsNullOrWhiteSpace(mapping))
                    return StatementParser.Preview(format, content!, JsonSerializer.Deserialize<CsvMapping>(mapping, JsonOpts), force: true);
                var saved = bank.CsvMappingJson is null ? null : JsonSerializer.Deserialize<CsvMapping>(bank.CsvMappingJson, JsonOpts);
                return StatementParser.Preview(format, content!, saved);
            }
            catch (JsonException)
            {
                return Problem("El mapeo de columnas no es válido.", statusCode: 400);
            }
        }

        /// <summary>Imports a CSV/OFX/QFX statement. For CSV, <paramref name="mapping"/> is the column mapping as JSON (optional: auto-detected).</summary>
        [HttpPost("{id:int}/statements")]
        [RequestSizeLimit(StatementParser.MaxFileBytes + 64 * 1024)]
        public async Task<ActionResult<ImportResultDto>> Import(int companyId, int id, IFormFile file, [FromForm] string? mapping)
        {
            var bank = await _db.BankAccounts.FirstOrDefaultAsync(b => b.CompanyClientId == companyId && b.Id == id);
            if (bank is null) return NotFound();
            var (content, error) = await ReadFile(file);
            if (error != null) return error;

            var format = StatementParser.DetectFormat(file.FileName, content!);
            ParseResult parsed;
            if (format == "ofx") parsed = StatementParser.ParseOfx(content!);
            else
            {
                CsvMapping map;
                try
                {
                    map = string.IsNullOrWhiteSpace(mapping)
                        ? StatementParser.SuggestMapping(StatementParser.ReadCsv(content!))
                        : JsonSerializer.Deserialize<CsvMapping>(mapping, JsonOpts) ?? throw new JsonException();
                }
                catch (JsonException)
                {
                    return Problem("El mapeo de columnas no es válido.", statusCode: 400);
                }
                parsed = StatementParser.ParseCsv(content!, map);
                if (parsed.Lines.Count > 0) bank.CsvMappingJson = JsonSerializer.Serialize(map, JsonOpts);
            }

            if (parsed.Lines.Count == 0)
            {
                var detail = parsed.Errors.Count > 0
                    ? $"No se pudo leer ningún movimiento. Primer error (fila {parsed.Errors[0].RowNumber}): {parsed.Errors[0].Message}"
                    : "El archivo no contiene movimientos.";
                return Problem(detail, statusCode: 400);
            }

            var keys = StatementParser.DedupeKeys(parsed.Lines);
            var from = parsed.Lines.Min(l => l.Date);
            var to = parsed.Lines.Max(l => l.Date);
            var existing = (await _db.BankTransactions
                .Where(t => t.BankAccountId == id && t.Date >= from && t.Date <= to)
                .Select(t => t.DedupeKey).ToListAsync()).ToHashSet();

            var fresh = parsed.Lines.Zip(keys).Where(p => !existing.Contains(p.Second)).DistinctBy(p => p.Second).ToList();
            var duplicates = parsed.Lines.Count - fresh.Count;
            var errors = parsed.Errors.Take(50).ToList();

            if (fresh.Count == 0)
            {
                await _db.SaveChangesAsync(); // keep the remembered mapping
                return new ImportResultDto(null, 0, duplicates, from, to, errors);
            }

            using var trx = await _db.Database.BeginTransactionAsync();
            var statement = new BankStatement
            {
                BankAccountId = id,
                FileName = Path.GetFileName(file.FileName) is { Length: > 0 } n ? (n.Length > 260 ? n[..260] : n) : "extracto",
                Format = format,
                ImportedAt = DateTime.UtcNow,
                ImportedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                FromDate = fresh.Min(p => p.First.Date),
                ToDate = fresh.Max(p => p.First.Date),
                ImportedCount = fresh.Count,
                DuplicateCount = duplicates,
            };
            _db.BankStatements.Add(statement);
            await _db.SaveChangesAsync();

            _db.BankTransactions.AddRange(fresh.Select(p => new BankTransaction
            {
                BankAccountId = id,
                BankStatementId = statement.Id,
                Date = p.First.Date,
                Description = p.First.Description,
                Reference = p.First.Reference,
                Amount = p.First.Amount,
                Balance = p.First.Balance,
                ExternalId = p.First.ExternalId,
                DedupeKey = p.Second,
            }));
            Audit.Log(_db, User, companyId, "StatementImported", "BankStatement", statement.Id,
                $"Extracto {statement.FileName} en {bank.Name}: {fresh.Count} movimientos nuevos, {duplicates} omitidos");
            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            // Rules marked "reconcile automatically" run on the new lines right away.
            var newIds = await _db.BankTransactions.Where(t => t.BankStatementId == statement.Id).Select(t => t.Id).ToListAsync();
            var auto = await ReconciliationService.AutoReconcileAsync(_db, companyId, id, newIds, User);

            return new ImportResultDto(statement.Id, fresh.Count, duplicates, statement.FromDate, statement.ToDate, errors, auto);
        }

        /// <summary>Runs the auto-reconcile rules over every pending line of the account.</summary>
        [HttpPost("{id:int}/apply-rules")]
        public async Task<ActionResult<object>> ApplyRules(int companyId, int id)
        {
            if (!await _db.BankAccounts.AnyAsync(b => b.CompanyClientId == companyId && b.Id == id)) return NotFound();
            var reconciled = await ReconciliationService.AutoReconcileAsync(_db, companyId, id, null, User);
            return new { reconciled };
        }

        [HttpGet("{id:int}/statements")]
        public async Task<ActionResult<List<BankStatement>>> Statements(int companyId, int id)
        {
            if (!await _db.BankAccounts.AnyAsync(b => b.CompanyClientId == companyId && b.Id == id)) return NotFound();
            return await _db.BankStatements.AsNoTracking().Where(s => s.BankAccountId == id).OrderByDescending(s => s.ImportedAt).ToListAsync();
        }

        /// <summary>Removes an import and its lines. Only allowed while none of its lines is reconciled.</summary>
        [HttpDelete("{id:int}/statements/{statementId:int}")]
        public async Task<IActionResult> DeleteStatement(int companyId, int id, int statementId)
        {
            var statement = await _db.BankStatements.AsNoTracking().FirstOrDefaultAsync(s => s.Id == statementId && s.BankAccountId == id && s.BankAccount!.CompanyClientId == companyId);
            if (statement is null) return NotFound();
            if (await _db.BankTransactions.AnyAsync(t => t.BankStatementId == statementId && t.Status == BankTransactionStatus.Reconciled))
                return Problem("Este extracto tiene movimientos conciliados. Deshaz esas conciliaciones primero.", statusCode: 409);

            using var trx = await _db.Database.BeginTransactionAsync();
            await _db.BankTransactions.Where(t => t.BankStatementId == statementId).ExecuteDeleteAsync();
            await _db.BankStatements.Where(s => s.Id == statementId).ExecuteDeleteAsync();
            Audit.Log(_db, User, companyId, "StatementDeleted", "BankStatement", statementId, $"Extracto eliminado: {statement.FileName} ({statement.ImportedCount} movimientos)");
            await _db.SaveChangesAsync();
            await trx.CommitAsync();
            return NoContent();
        }

        // ------------------------------------------------------------ transactions

        [HttpGet("{id:int}/transactions")]
        public async Task<ActionResult<PagedResult<BankTransactionDto>>> Transactions(
            int companyId, int id, [FromQuery] BankTransactionStatus? status, [FromQuery] string? q,
            [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            var bank = await _db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(b => b.CompanyClientId == companyId && b.Id == id);
            if (bank is null) return NotFound();
            page = Math.Max(page, 1);
            limit = Math.Clamp(limit, 1, 200);

            var query = _db.BankTransactions.AsNoTracking().Where(t => t.BankAccountId == id);
            if (status != null) query = query.Where(t => t.Status == status);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(t => t.Description.Contains(q) || (t.Reference != null && t.Reference.Contains(q)));

            var count = await query.CountAsync();
            // To reconcile, oldest first (like Xero); history, newest first.
            query = status == BankTransactionStatus.Unreconciled
                ? query.OrderBy(t => t.Date).ThenBy(t => t.Id)
                : query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id);
            var items = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();

            var dtos = await BankTransactionMapper.ToDtos(_db, companyId, bank, items);
            return new PagedResult<BankTransactionDto>(dtos, new PaginationModel(limit, page, count));
        }

        // ------------------------------------------------------------ helpers

        private Task<BankAccountDto?> ToDto(int companyId, int id) => ToDtoAsync(_db, companyId, id);

        /// <summary>Bank account with statement balance, ledger balance and pending count (also used by the dashboard).</summary>
        internal static async Task<BankAccountDto?> ToDtoAsync(ApplicationDBContext _db, int companyId, int id)
        {
            var b = await _db.BankAccounts.AsNoTracking().Include(x => x.Account).FirstOrDefaultAsync(x => x.CompanyClientId == companyId && x.Id == id);
            if (b is null) return null;

            var txs = _db.BankTransactions.Where(t => t.BankAccountId == id);
            var last = await txs.Where(t => t.Balance != null).OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
                .Select(t => new { t.Date, t.Balance }).FirstOrDefaultAsync();
            var lastDate = await txs.MaxAsync(t => (DateOnly?)t.Date);
            // With a running balance column the bank's own figure is used; otherwise the sum of imported lines.
            var statementBalance = last != null && last.Date == lastDate ? last.Balance!.Value : await txs.SumAsync(t => (decimal?)t.Amount) ?? 0;
            var ledger = await _db.JournalLines.Where(l => l.AccountId == b.AccountId).SumAsync(l => (decimal?)(l.Debit - l.Credit)) ?? 0;
            var unreconciled = await txs.CountAsync(t => t.Status == BankTransactionStatus.Unreconciled);
            var lastImport = await _db.BankStatements.Where(s => s.BankAccountId == id).MaxAsync(s => (DateTime?)s.ImportedAt);

            return new BankAccountDto(b.Id, b.Name, b.BankName, b.BSB, b.AccountNumber, b.Currency, b.AccountId, b.Account!.Code,
                statementBalance, lastDate, ledger, unreconciled, lastImport);
        }

        private async Task<(string? Content, ActionResult? Error)> ReadFile(IFormFile? file)
        {
            if (file is null || file.Length == 0) return (null, Problem("Selecciona un archivo.", statusCode: 400));
            if (file.Length > StatementParser.MaxFileBytes) return (null, Problem("El archivo supera los 5 MB.", statusCode: 400));
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".csv" or ".txt" or ".ofx" or ".qfx"))
                return (null, Problem("Formato no soportado. Sube un CSV, OFX o QFX exportado desde el banco.", statusCode: 400));
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return (StatementParser.ReadText(ms.ToArray()), null);
        }

        private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
