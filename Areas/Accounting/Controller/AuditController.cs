using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.DataBase;
using Accounting_Register_BE.Resources.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    /// <summary>Read-only audit trail of a company: who did what and when.</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/audit")]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public AuditController(ApplicationDBContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<PagedResult<AuditLog>>> List(int companyId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
            [FromQuery] string? action, [FromQuery] string? entityType, [FromQuery] string? entityId, [FromQuery] string? q,
            [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            page = Math.Max(page, 1);
            limit = Math.Clamp(limit, 1, 200);
            var query = _db.AuditLogs.AsNoTracking().Where(a => a.CompanyClientId == companyId);
            if (from != null) query = query.Where(a => a.At >= from.Value.ToDateTime(TimeOnly.MinValue));
            if (to != null) query = query.Where(a => a.At < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
            if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
            if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(a => a.EntityId == entityId);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(a => a.Summary.Contains(q) || a.Actor.Contains(q));
            var count = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.At).ThenByDescending(a => a.Id).Skip((page - 1) * limit).Take(limit).ToListAsync();
            return new PagedResult<AuditLog>(items, new PaginationModel(limit, page, count));
        }
    }
}
