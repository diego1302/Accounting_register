using Accounting_Register_BE.Areas.Security_Issuer.Model;
using Accounting_Register_BE.DataBase;
using Accounting_Register_BE.Resources.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Config.Security_Issuer.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/securities-issuers")]
    public class SecuritiesIssuersController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public SecuritiesIssuersController(ApplicationDBContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<SecuritiesIssuer>>> List([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int limit = 25)
        {
            page = Math.Max(page, 1);
            limit = Math.Clamp(limit, 1, 200);

            var query = _db.SecurtiesIssuer.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(x => x.Name.Contains(q) || (x.Code != null && x.Code.Contains(q)) || (x.email != null && x.email.Contains(q)));
            }

            var count = await query.CountAsync();
            var items = await query.OrderBy(x => x.Name).Skip((page - 1) * limit).Take(limit).ToListAsync();
            return new PagedResult<SecuritiesIssuer>(items, new PaginationModel(limit, page, count));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SecuritiesIssuer>> Get(int id)
        {
            var entity = await _db.SecurtiesIssuer.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return entity is null ? NotFound() : entity;
        }

        [HttpPost]
        public async Task<ActionResult<SecuritiesIssuer>> Create(SecuritiesIssuer model)
        {
            model.Id = 0;
            NormaliseAddresses(model);
            _db.SecurtiesIssuer.Add(model);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<SecuritiesIssuer>> Update(int id, SecuritiesIssuer model)
        {
            var entity = await _db.SecurtiesIssuer.FirstOrDefaultAsync(x => x.Id == id);
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
            var entity = await _db.SecurtiesIssuer.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();
            _db.SecurtiesIssuer.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    
        // JSON clients may send null/partial addresses; the DB mapping expects every level to exist.
        private static void NormaliseAddresses(SecuritiesIssuer model)
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
