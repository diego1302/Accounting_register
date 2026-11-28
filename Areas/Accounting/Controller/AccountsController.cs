using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.Areas.Accounting.Services;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    /// <summary>Chart of accounts of one company client.</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/accounts")]
    public class AccountsController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public AccountsController(ApplicationDBContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<List<Account>>> List(int companyId, [FromQuery] string? q, [FromQuery] bool includeArchived = false)
        {
            if (!await CompanyExists(companyId)) return CompanyNotFound();
            var query = _db.Accounts.AsNoTracking().Where(a => a.CompanyClientId == companyId);
            if (!includeArchived) query = query.Where(a => !a.IsArchived);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(a => a.Code.Contains(q) || a.Name.Contains(q));
            return await query.OrderBy(a => a.Code).ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Account>> Get(int companyId, int id)
        {
            var entity = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.CompanyClientId == companyId && a.Id == id);
            return entity is null ? NotFound() : entity;
        }

        [HttpPost]
        public async Task<ActionResult<Account>> Create(int companyId, AccountInput input)
        {
            if (!await CompanyExists(companyId)) return CompanyNotFound();
            if (input.Type == AccountType.Bank) return Problem("Las cuentas de tipo Banco se crean desde Bancos.", statusCode: 400);
            var code = input.Code.Trim();
            if (await _db.Accounts.AnyAsync(a => a.CompanyClientId == companyId && a.Code == code))
                return Problem($"Ya existe una cuenta con el código {code}.", statusCode: 409);

            var entity = new Account
            {
                CompanyClientId = companyId,
                Code = code,
                Name = input.Name.Trim(),
                Type = input.Type,
                TaxType = input.TaxType,
                Description = input.Description,
                IsArchived = input.IsArchived,
            };
            _db.Accounts.Add(entity);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { companyId, id = entity.Id }, entity);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<Account>> Update(int companyId, int id, AccountInput input)
        {
            var entity = await _db.Accounts.FirstOrDefaultAsync(a => a.CompanyClientId == companyId && a.Id == id);
            if (entity is null) return NotFound();

            var code = input.Code.Trim();
            if (code != entity.Code && await _db.Accounts.AnyAsync(a => a.CompanyClientId == companyId && a.Code == code && a.Id != id))
                return Problem($"Ya existe una cuenta con el código {code}.", statusCode: 409);

            // Bank and system accounts keep their type; changing it would break reconciliation / GST posting.
            if (entity.Type == AccountType.Bank || entity.SystemKey != null)
            {
                if (input.Type != entity.Type) return Problem("No se puede cambiar el tipo de esta cuenta.", statusCode: 400);
                if (input.IsArchived) return Problem("Esta cuenta no se puede archivar.", statusCode: 400);
            }
            else if (input.Type == AccountType.Bank) return Problem("Las cuentas de tipo Banco se crean desde Bancos.", statusCode: 400);

            entity.Code = code;
            entity.Name = input.Name.Trim();
            entity.Type = input.Type;
            entity.TaxType = input.TaxType;
            entity.Description = input.Description;
            entity.IsArchived = input.IsArchived;
            await _db.SaveChangesAsync();
            return entity;
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int companyId, int id)
        {
            var entity = await _db.Accounts.FirstOrDefaultAsync(a => a.CompanyClientId == companyId && a.Id == id);
            if (entity is null) return NotFound();
            if (entity.Type == AccountType.Bank) return Problem("Esta cuenta pertenece a una cuenta bancaria; elimínala desde Bancos.", statusCode: 409);
            if (entity.SystemKey != null) return Problem("Esta cuenta la usa el sistema y no se puede eliminar.", statusCode: 409);
            if (await _db.JournalLines.AnyAsync(l => l.AccountId == id))
                return Problem("La cuenta tiene movimientos contabilizados. Puedes archivarla en lugar de eliminarla.", statusCode: 409);

            _db.Accounts.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Loads the standard Australian chart; codes already in use are skipped.</summary>
        [HttpPost("load-default")]
        public async Task<ActionResult<object>> LoadDefault(int companyId)
        {
            if (!await CompanyExists(companyId)) return CompanyNotFound();
            var added = await AccountingService.LoadDefaultChartAsync(_db, companyId);
            return new { added };
        }

        private Task<bool> CompanyExists(int companyId) => _db.CompanyClients.AnyAsync(c => c.Id == companyId);
        private ObjectResult CompanyNotFound() => Problem("La empresa no existe.", statusCode: 404);
    }
}
