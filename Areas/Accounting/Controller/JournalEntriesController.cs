using System.Security.Claims;

using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.Areas.Accounting.Security;
using Accounting_Register_BE.DataBase;
using Accounting_Register_BE.Resources.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    /// <summary>
    /// Journal entries of a company. Used by the app (user bearer token) and by external applications
    /// (header <c>X-Api-Key</c>, key scoped to the company) to post debits and credits.
    /// </summary>
    [Authorize(Policy = JournalEntriesController.Policy)]
    [ApiController]
    [Route("api/companies/{companyId:int}/journal-entries")]
    public class JournalEntriesController : ControllerBase
    {
        /// <summary>Accepts Identity bearer tokens and API keys.</summary>
        public const string Policy = "JournalApi";
        public const int MaxLines = 500;
        public const int MaxBatch = 100;

        private readonly ApplicationDBContext _db;

        public JournalEntriesController(ApplicationDBContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<PagedResult<JournalEntryDetailDto>>> List(
            int companyId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] JournalSource? source,
            [FromQuery] string? externalSource, [FromQuery] string? externalId, [FromQuery] string? q,
            [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (Denied(companyId) is { } denied) return denied;
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            page = Math.Max(page, 1);
            limit = Math.Clamp(limit, 1, 200);

            var query = _db.JournalEntries.AsNoTracking().Where(e => e.CompanyClientId == companyId);
            if (from != null) query = query.Where(e => e.Date >= from);
            if (to != null) query = query.Where(e => e.Date <= to);
            if (source != null) query = query.Where(e => e.Source == source);
            if (!string.IsNullOrWhiteSpace(externalSource)) query = query.Where(e => e.ExternalSource == externalSource);
            if (!string.IsNullOrWhiteSpace(externalId)) query = query.Where(e => e.ExternalId == externalId);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(e => e.Narration.Contains(q) || (e.Reference != null && e.Reference.Contains(q)) || (e.ExternalId != null && e.ExternalId.Contains(q)));

            var count = await query.CountAsync();
            var entries = await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
                .Skip((page - 1) * limit).Take(limit)
                .Include(e => e.Lines).ThenInclude(l => l.Account).ToListAsync();
            return new PagedResult<JournalEntryDetailDto>(entries.Select(ToDto).ToList(), new PaginationModel(limit, page, count));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<JournalEntryDetailDto>> Get(int companyId, int id)
        {
            if (Denied(companyId) is { } denied) return denied;
            var entry = await Load(companyId, id);
            return entry is null ? NotFound() : ToDto(entry);
        }

        /// <summary>
        /// Posts one balanced entry. Returns 201 when created, or 200 with the existing entry when
        /// ExternalSource + ExternalId was already posted (safe to retry).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<JournalEntryDetailDto>> Create(int companyId, JournalEntryInput input)
        {
            if (Denied(companyId) is { } denied) return denied;
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);

            var accounts = await AccountsFor(companyId, new[] { input });
            var errors = Validate(input, accounts, "");
            if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors) { Title = "El asiento no es válido." });

            var existing = await FindExisting(companyId, input);
            if (existing != null) return Ok(ToDto(existing));

            var entry = Build(companyId, input, accounts);
            _db.JournalEntries.Add(entry);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException) when (input.ExternalId != null)
            {
                // Same ExternalId posted concurrently: the unique index kept only one, return it.
                _db.ChangeTracker.Clear();
                existing = await FindExisting(companyId, input);
                if (existing != null) return Ok(ToDto(existing));
                throw;
            }

            return CreatedAtAction(nameof(Get), new { companyId, id = entry.Id }, ToDto((await Load(companyId, entry.Id))!));
        }

        /// <summary>Posts up to 100 entries all-or-nothing: if any entry is invalid nothing is saved.</summary>
        [HttpPost("batch")]
        public async Task<ActionResult<JournalBatchResult>> CreateBatch(int companyId, JournalBatchInput input)
        {
            if (Denied(companyId) is { } denied) return denied;
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            if (input.Entries.Count == 0) return Problem("El lote no contiene asientos.", statusCode: 400);
            if (input.Entries.Count > MaxBatch) return Problem($"Máximo {MaxBatch} asientos por lote.", statusCode: 400);

            var accounts = await AccountsFor(companyId, input.Entries);
            var errors = new Dictionary<string, string[]>();
            for (var i = 0; i < input.Entries.Count; i++)
                foreach (var (k, v) in Validate(input.Entries[i], accounts, $"entries[{i}]")) errors[k] = v;
            var dupes = input.Entries.Select((e, i) => (e, i)).Where(x => x.e.ExternalId != null)
                .GroupBy(x => (Source(x.e), x.e.ExternalId)).Where(g => g.Count() > 1);
            foreach (var g in dupes) errors[$"entries[{g.Last().i}].externalId"] = new[] { $"ExternalId \"{g.Key.ExternalId}\" repetido en el lote." };
            if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors) { Title = "El lote no es válido; no se guardó ningún asiento." });

            var results = new List<(int Index, string Status, JournalEntry Entry)>();
            using var trx = await _db.Database.BeginTransactionAsync();
            for (var i = 0; i < input.Entries.Count; i++)
            {
                var existing = await FindExisting(companyId, input.Entries[i]);
                if (existing != null) { results.Add((i, "existing", existing)); continue; }
                var entry = Build(companyId, input.Entries[i], accounts);
                _db.JournalEntries.Add(entry);
                results.Add((i, "created", entry));
            }
            try
            {
                await _db.SaveChangesAsync();
                await trx.CommitAsync();
            }
            catch (DbUpdateException)
            {
                return Problem("Otro envío registró alguno de estos ExternalId al mismo tiempo. Reintenta el lote: los ya registrados se devolverán como existentes.", statusCode: 409);
            }

            var ids = results.Select(r => r.Entry.Id).ToList();
            var loaded = await _db.JournalEntries.AsNoTracking().Where(e => ids.Contains(e.Id))
                .Include(e => e.Lines).ThenInclude(l => l.Account).ToDictionaryAsync(e => e.Id);
            var items = results.Select(r => new JournalBatchItemResult(r.Index, r.Status, ToDto(loaded[r.Entry.Id]))).ToList();
            return new JournalBatchResult(items.Count(x => x.Status == "created"), items.Count(x => x.Status == "existing"), items);
        }

        /// <summary>Deletes a manual or external entry. Bank entries are undone from the reconciliation instead.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int companyId, int id)
        {
            if (Denied(companyId) is { } denied) return denied;
            var entry = await _db.JournalEntries.FirstOrDefaultAsync(e => e.CompanyClientId == companyId && e.Id == id);
            if (entry is null) return NotFound();
            if (entry.Source == JournalSource.Bank || await _db.BankTransactions.AnyAsync(t => t.JournalEntryId == id))
                return Problem("Este asiento viene de una conciliación bancaria; deshazlo desde Bancos.", statusCode: 409);
            if (User.ApiKeyId() != null && entry.Source != JournalSource.External)
                return Problem("Una API key solo puede eliminar asientos externos.", statusCode: 403);

            _db.JournalEntries.Remove(entry); // lines cascade
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ------------------------------------------------------------ helpers

        /// <summary>An API key only reaches its own company.</summary>
        private ObjectResult? Denied(int companyId)
        {
            var keyCompany = User.ApiKeyCompanyId();
            return keyCompany != null && keyCompany != companyId
                ? Problem("La API key no pertenece a esta empresa.", statusCode: 403)
                : null;
        }

        private string? Source(JournalEntryInput input)
        {
            var s = string.IsNullOrWhiteSpace(input.ExternalSource) ? (User.ApiKeyId() != null ? User.Identity?.Name : null) : input.ExternalSource.Trim();
            return s is null ? null : s.Length > 50 ? s[..50] : s;
        }

        private async Task<JournalEntry?> FindExisting(int companyId, JournalEntryInput input)
        {
            if (string.IsNullOrWhiteSpace(input.ExternalId)) return null;
            var source = Source(input);
            var externalId = input.ExternalId.Trim();
            var id = await _db.JournalEntries.Where(e => e.CompanyClientId == companyId && e.ExternalSource == source && e.ExternalId == externalId)
                .Select(e => (int?)e.Id).FirstOrDefaultAsync();
            return id is null ? null : await Load(companyId, id.Value);
        }

        private Task<JournalEntry?> Load(int companyId, int id) =>
            _db.JournalEntries.AsNoTracking().Include(e => e.Lines).ThenInclude(l => l.Account)
                .FirstOrDefaultAsync(e => e.CompanyClientId == companyId && e.Id == id);

        private async Task<List<Account>> AccountsFor(int companyId, IEnumerable<JournalEntryInput> entries)
        {
            var lines = entries.SelectMany(e => e.Lines ?? new()).ToList();
            var ids = lines.Where(l => l.AccountId != null).Select(l => l.AccountId!.Value).Distinct().ToList();
            var codes = lines.Where(l => !string.IsNullOrWhiteSpace(l.AccountCode)).Select(l => l.AccountCode!.Trim()).Distinct().ToList();
            return await _db.Accounts.AsNoTracking()
                .Where(a => a.CompanyClientId == companyId && (ids.Contains(a.Id) || codes.Contains(a.Code))).ToListAsync();
        }

        private static Account? Resolve(JournalLineInput l, List<Account> accounts) =>
            l.AccountId != null ? accounts.FirstOrDefault(a => a.Id == l.AccountId)
            : !string.IsNullOrWhiteSpace(l.AccountCode) ? accounts.FirstOrDefault(a => a.Code == l.AccountCode.Trim()) : null;

        private static Dictionary<string, string[]> Validate(JournalEntryInput input, List<Account> accounts, string prefix)
        {
            var errors = new Dictionary<string, string[]>();
            string P(string field) => prefix.Length == 0 ? field : $"{prefix}.{field}";
            void Add(string field, string msg) => errors[P(field)] = new[] { msg };

            if (input.Date == default) Add("date", "La fecha es obligatoria (yyyy-MM-dd).");
            if (string.IsNullOrWhiteSpace(input.Narration)) Add("narration", "La descripción (narration) es obligatoria.");
            var lines = input.Lines ?? new();
            if (lines.Count < 2) Add("lines", "Un asiento necesita al menos dos líneas (débito y crédito).");
            if (lines.Count > MaxLines) Add("lines", $"Máximo {MaxLines} líneas por asiento.");

            for (var i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                var at = $"lines[{i}]";
                if (l.AccountId == null && string.IsNullOrWhiteSpace(l.AccountCode)) { Add($"{at}.accountCode", "Indica accountCode o accountId."); continue; }
                var acc = Resolve(l, accounts);
                if (acc is null) Add($"{at}.accountCode", $"La cuenta {(l.AccountId?.ToString() ?? l.AccountCode)} no existe en esta empresa.");
                else if (acc.IsArchived) Add($"{at}.accountCode", $"La cuenta {acc.Code} {acc.Name} está archivada.");
                if (l.Debit < 0 || l.Credit < 0) Add($"{at}.debit", "Los montos no pueden ser negativos.");
                else if ((l.Debit > 0) == (l.Credit > 0)) Add($"{at}.debit", "Cada línea lleva débito o crédito (uno de los dos, mayor que cero).");
                if (Math.Round(l.Debit, 2) != l.Debit || Math.Round(l.Credit, 2) != l.Credit) Add($"{at}.debit", "Máximo 2 decimales.");
            }

            if (!errors.Keys.Any(k => k.StartsWith(P("lines"))))
            {
                var debit = lines.Sum(l => l.Debit);
                var credit = lines.Sum(l => l.Credit);
                if (debit != credit) Add("lines", $"El asiento no cuadra: débitos {debit:0.00} ≠ créditos {credit:0.00}.");
            }
            return errors;
        }

        private JournalEntry Build(int companyId, JournalEntryInput input, List<Account> accounts)
        {
            var viaKey = User.ApiKeyId();
            return new JournalEntry
            {
                CompanyClientId = companyId,
                Date = input.Date,
                Reference = string.IsNullOrWhiteSpace(input.Reference) ? null : input.Reference.Trim(),
                Narration = input.Narration.Trim(),
                Source = viaKey != null ? JournalSource.External : JournalSource.Manual,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = viaKey == null ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
                ApiKeyId = viaKey,
                ExternalSource = string.IsNullOrWhiteSpace(input.ExternalId) && string.IsNullOrWhiteSpace(input.ExternalSource) && viaKey == null ? null : Source(input),
                ExternalId = string.IsNullOrWhiteSpace(input.ExternalId) ? null : input.ExternalId.Trim(),
                Lines = input.Lines.Select(l => new JournalLine
                {
                    AccountId = Resolve(l, accounts)!.Id,
                    Description = string.IsNullOrWhiteSpace(l.Description) ? null : l.Description.Trim(),
                    Debit = l.Debit,
                    Credit = l.Credit,
                    TaxType = l.TaxType,
                }).ToList(),
            };
        }

        public static JournalEntryDetailDto ToDto(JournalEntry e) => new(
            e.Id, e.Date, e.Reference, e.Narration, e.Source, e.ExternalSource, e.ExternalId, e.CreatedAt,
            e.Lines.Sum(l => l.Debit), e.Lines.Sum(l => l.Credit),
            e.Lines.OrderBy(l => l.Id).Select(l => new JournalLineDto(l.AccountId, l.Account!.Code, l.Account.Name, l.Description, l.Debit, l.Credit, l.TaxType)).ToList());
    }
}
