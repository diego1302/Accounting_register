using Accounting_Register_BE.DataBase;
using Accounting_Register_BE.Resources.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Resources.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/countries")]
    public class CountriesController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public CountriesController(ApplicationDBContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<Countries>>> List([FromQuery] string? q)
        {
            var query = _db.Countries.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(x => x.shortName.Contains(q) || x.full_name.Contains(q) || x.aplha2code == q || x.aplha3code == q);
            }
            return await query.OrderBy(x => x.shortName).ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Countries>> Get(int id)
        {
            var entity = await _db.Countries.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return entity is null ? NotFound() : entity;
        }

        [HttpPost]
        public async Task<ActionResult<Countries>> Create(Countries model)
        {
            model.id = 0;
            _db.Countries.Add(model);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.id }, model);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<Countries>> Update(int id, Countries model)
        {
            var entity = await _db.Countries.FirstOrDefaultAsync(x => x.id == id);
            if (entity is null) return NotFound();
            entity.aplha2code = model.aplha2code;
            entity.aplha3code = model.aplha3code;
            entity.numericcode = model.numericcode;
            entity.shortName = model.shortName;
            entity.short_name_lower_case = model.short_name_lower_case;
            entity.full_name = model.full_name;
            await _db.SaveChangesAsync();
            return entity;
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Countries.FirstOrDefaultAsync(x => x.id == id);
            if (entity is null) return NotFound();
            _db.Countries.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
