using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.Areas.Accounting.Services;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Areas.Accounting.Controller
{
    /// <summary>Bank rules: propose (or automatically post) the account for lines whose description/reference matches.</summary>
    [Authorize]
    [ApiController]
    [Route("api/companies/{companyId:int}/bank-rules")]
    public class BankRulesController : ControllerBase
    {
        private readonly ApplicationDBContext _db;

        public BankRulesController(ApplicationDBContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<List<BankRule>>> List(int companyId)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            return await _db.BankRules.AsNoTracking().Where(r => r.CompanyClientId == companyId).OrderBy(r => r.Priority).ThenBy(r => r.Id).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<BankRule>> Create(int companyId, BankRuleInput input)
        {
            if (!await _db.CompanyClients.AnyAsync(c => c.Id == companyId)) return Problem("La empresa no existe.", statusCode: 404);
            if (await Validate(companyId, input) is { } problem) return problem;
            var rule = new BankRule { CompanyClientId = companyId };
            Apply(rule, input);
            _db.BankRules.Add(rule);
            await _db.SaveChangesAsync();
            Audit.Log(_db, User, companyId, "RuleCreated", "BankRule", rule.Id, $"Regla creada: {rule.Name} («{rule.MatchText}»{(rule.AutoReconcile ? ", automática" : "")})");
            await _db.SaveChangesAsync();
            return rule;
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<BankRule>> Update(int companyId, int id, BankRuleInput input)
        {
            var rule = await _db.BankRules.FirstOrDefaultAsync(r => r.CompanyClientId == companyId && r.Id == id);
            if (rule is null) return NotFound();
            if (await Validate(companyId, input) is { } problem) return problem;
            Apply(rule, input);
            Audit.Log(_db, User, companyId, "RuleUpdated", "BankRule", rule.Id, $"Regla modificada: {rule.Name}{(rule.IsActive ? "" : " (inactiva)")}");
            await _db.SaveChangesAsync();
            return rule;
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int companyId, int id)
        {
            var rule = await _db.BankRules.FirstOrDefaultAsync(r => r.CompanyClientId == companyId && r.Id == id);
            if (rule is null) return NotFound();
            _db.BankRules.Remove(rule);
            Audit.Log(_db, User, companyId, "RuleDeleted", "BankRule", id, $"Regla eliminada: {rule.Name}");
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private async Task<ObjectResult?> Validate(int companyId, BankRuleInput input)
        {
            if (string.IsNullOrWhiteSpace(input.MatchText)) return Problem("Indica el texto a buscar.", statusCode: 400);
            if (input.AmountMin < 0 || input.AmountMax < 0 || (input.AmountMin != null && input.AmountMax != null && input.AmountMin > input.AmountMax))
                return Problem("El rango de montos no es válido.", statusCode: 400);
            if (input.BankAccountId != null && !await _db.BankAccounts.AnyAsync(b => b.Id == input.BankAccountId && b.CompanyClientId == companyId))
                return Problem("La cuenta bancaria no existe en esta empresa.", statusCode: 400);
            var acc = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == input.AccountId && a.CompanyClientId == companyId);
            if (acc is null) return Problem("La cuenta contable no existe en esta empresa.", statusCode: 400);
            if (acc.IsArchived) return Problem($"La cuenta {acc.Code} está archivada.", statusCode: 400);
            if (acc.Type == AccountType.Bank) return Problem("Para movimientos entre bancos usa «Transferencia» al conciliar, no una regla.", statusCode: 400);
            if (acc.SystemKey != null) return Problem("Esa cuenta la usa el sistema; elige la cuenta de gasto o ingreso.", statusCode: 400);
            return null;
        }

        private static void Apply(BankRule r, BankRuleInput i)
        {
            r.BankAccountId = i.BankAccountId;
            r.Name = i.Name.Trim();
            r.Priority = i.Priority;
            r.MatchField = i.MatchField;
            r.MatchType = i.MatchType;
            r.MatchText = i.MatchText.Trim();
            r.Direction = i.Direction;
            r.AmountMin = i.AmountMin;
            r.AmountMax = i.AmountMax;
            r.AccountId = i.AccountId;
            r.TaxType = i.TaxType;
            r.LineDescription = string.IsNullOrWhiteSpace(i.LineDescription) ? null : i.LineDescription.Trim();
            r.AutoReconcile = i.AutoReconcile;
            r.IsActive = i.IsActive;
        }
    }
}
