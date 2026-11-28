using System.Security.Claims;
using System.Text.Json;

using Accounting_Register_BE.Areas.Accounting.Model;
using Accounting_Register_BE.Areas.Accounting.Security;
using Accounting_Register_BE.DataBase;

namespace Accounting_Register_BE.Areas.Accounting.Services
{
    /// <summary>
    /// Adds audit rows to the context; they are saved by the caller's SaveChanges, so the log and the change
    /// it describes are committed together.
    /// </summary>
    public static class Audit
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        /// <summary>"ana@x.com", "API: POS tienda" or "Sistema".</summary>
        public static string ActorName(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true) return "Sistema";
            var name = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
            return user.ApiKeyId() != null ? $"API: {name}" : name;
        }

        public static void Log(ApplicationDBContext db, ClaimsPrincipal? user, int? companyId, string action, string entityType, object? entityId,
            string summary, object? details = null, string? actor = null)
        {
            db.AuditLogs.Add(new AuditLog
            {
                CompanyClientId = companyId,
                At = DateTime.UtcNow,
                UserId = user?.ApiKeyId() == null ? user?.FindFirstValue(ClaimTypes.NameIdentifier) : null,
                ApiKeyId = user?.ApiKeyId(),
                Actor = Truncate(actor ?? ActorName(user), 256),
                Action = action,
                EntityType = entityType,
                EntityId = entityId?.ToString(),
                Summary = Truncate(summary, 500),
                Details = details is null ? null : JsonSerializer.Serialize(details, Json),
            });
        }

        /// <summary>Compact snapshot of a journal for the log (used when it is deleted).</summary>
        public static object Snapshot(JournalEntry e) => new
        {
            e.Id, e.Date, e.Reference, e.Narration, Source = e.Source.ToString(), e.ExternalSource, e.ExternalId,
            Lines = e.Lines.Select(l => new { l.AccountId, Code = l.Account?.Code, l.Description, l.Debit, l.Credit }),
        };

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
    }
}
