using Accounting_Register_BE.Areas.Config.Users.Model;
using Accounting_Register_BE.DataBase;
using Accounting_Register_BE.Resources.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Config.Users.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/company-clients")]
    public class CompanyClientsController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public CompanyClientsController(ApplicationDBContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<CompanyClient>>> List([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int limit = 25)
        {
            page = Math.Max(page, 1);
            limit = Math.Clamp(limit, 1, 200);

            var query = _db.CompanyClients.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(x => x.Name.Contains(q) || (x.Code != null && x.Code.Contains(q)) || (x.email != null && x.email.Contains(q)));
            }

            var count = await query.CountAsync();
            var items = await query.OrderBy(x => x.Name).Skip((page - 1) * limit).Take(limit).ToListAsync();
            return new PagedResult<CompanyClient>(items, new PaginationModel(limit, page, count));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CompanyClient>> Get(int id)
        {
            var entity = await _db.CompanyClients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return entity is null ? NotFound() : entity;
        }

        [HttpPost]
        public async Task<ActionResult<CompanyClient>> Create(CompanyClient model)
        {
            model.Id = 0;
            NormaliseAddresses(model);
            _db.CompanyClients.Add(model);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CompanyClient>> Update(int id, CompanyClient model)
        {
            var entity = await _db.CompanyClients.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();
            NormaliseAddresses(model);

            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.ABN = model.ABN;
            entity.ACN = model.ACN;
            entity.Code = model.Code;
            entity.phone_country_code = model.phone_country_code;
            entity.phone_number = model.phone_number;
            entity.email = model.email;
            entity.webpage = model.webpage;
            entity.registered_office_address = model.registered_office_address;
            entity.postal_office_address = model.postal_office_address;

            await _db.SaveChangesAsync();
            return entity;
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.CompanyClients.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();
            if (await _db.Accounts.AnyAsync(a => a.CompanyClientId == id) || await _db.JournalEntries.AnyAsync(j => j.CompanyClientId == id))
                return Problem("La empresa tiene contabilidad (plan de cuentas, bancos o asientos) y no se puede eliminar.", statusCode: 409);
            _db.CompanyClients.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    
        // JSON clients may send null/partial addresses; the DB mapping expects every level to exist.
        private static void NormaliseAddresses(CompanyClient model)
        {
            model.registered_office_address ??= new Addreses();
            model.postal_office_address ??= new Addreses();
            model.registered_office_address.australian_address ??= new AustralianAddress();
            model.registered_office_address.OverseasAddress ??= new OverseasAddress();
            model.postal_office_address.australian_address ??= new AustralianAddress();
            model.postal_office_address.OverseasAddress ??= new OverseasAddress();
        }
    }
}
