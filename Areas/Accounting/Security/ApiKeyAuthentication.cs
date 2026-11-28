using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Accounting_Register_BE.Areas.Accounting.Security
{
    /// <summary>
    /// "ApiKey" authentication for external applications: header <c>X-Api-Key: ar_…</c>.
    /// The principal carries the company the key belongs to; controllers must check it against the route.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ApiKey";
        public const string HeaderName = "X-Api-Key";
        public const string CompanyClaim = "company_id";
        public const string ApiKeyIdClaim = "api_key_id";

        private readonly ApplicationDBContext _db;

        public ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ApplicationDBContext db)
            : base(options, logger, encoder) => _db = db;

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var values)) return AuthenticateResult.NoResult();
            var key = values.ToString().Trim();
            if (key.Length == 0) return AuthenticateResult.NoResult();

            var hash = Hash(key);
            var apiKey = await _db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == hash);
            if (apiKey is null || apiKey.RevokedAt != null) return AuthenticateResult.Fail("API key inválida o revocada.");

            // Avoid a database write on every call.
            if (apiKey.LastUsedAt is null || apiKey.LastUsedAt < DateTime.UtcNow.AddMinutes(-5))
            {
                apiKey.LastUsedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, apiKey.Name),
                new Claim(CompanyClaim, apiKey.CompanyClientId.ToString()),
                new Claim(ApiKeyIdClaim, apiKey.Id.ToString()),
            }, SchemeName);
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
        }

        public static string Hash(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

        /// <summary>New random key: "ar_" + 43 url-safe characters (256 bits).</summary>
        public static string Generate() =>
            "ar_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static class ClaimsPrincipalExtensions
    {
        public static int? ApiKeyCompanyId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(ApiKeyAuthenticationHandler.CompanyClaim), out var id) ? id : null;

        public static int? ApiKeyId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(ApiKeyAuthenticationHandler.ApiKeyIdClaim), out var id) ? id : null;
    }
}
