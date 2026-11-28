using System.Security.Claims;

using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.Areas.Accounting.Security;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    /// <summary>API keys for external apps. Managed only by signed-in users (an API key cannot create keys).</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/api-keys")]
    public class ApiKeysController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public ApiKeysController(ApplicationDBContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<List<ApiKey>>> List(int companyId)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            return await _db.ApiKeys.AsNoTracking().Where(k => k.CompanyClientId == companyId)
                .OrderBy(k => k.RevokedAt != null).ThenByDescending(k => k.CreatedAt).ToListAsync();
        }

        /// <summary>Creates a key. The response is the only time the plain key is returned.</summary>
        [HttpPost]
        public async Task<ActionResult<ApiKeyCreatedDto>> Create(int companyId, ApiKeyInput input)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            var plain = ApiKeyAuthenticationHandler.Generate();
            var key = new ApiKey
            {
                CompanyClientId = companyId,
                Name = input.Name.Trim(),
                Prefix = plain[..9],
                KeyHash = ApiKeyAuthenticationHandler.Hash(plain),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            };
            _db.ApiKeys.Add(key);
            await _db.SaveChangesAsync();
            return new ApiKeyCreatedDto(key.Id, key.Name, key.Prefix, key.CreatedAt, plain);
        }

        /// <summary>Revokes a key immediately. It stays listed (revoked) so its entries remain traceable.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Revoke(int companyId, int id)
        {
            var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.CompanyClientId == companyId && k.Id == id);
            if (key is null) return NotFound();
            key.RevokedAt ??= DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
