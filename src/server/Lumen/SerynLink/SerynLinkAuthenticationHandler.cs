using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Seryn.Server.Persistence;

namespace Seryn.Server.Lumen.SerynLink;

public sealed class SerynLinkAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    SerynDbContext db,
    TimeProvider clock) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "SerynLink";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();
        var raw = authorization[7..].Trim();
        if (!raw.StartsWith("seryn_pat_", StringComparison.Ordinal) || raw.Length > 128)
            return AuthenticateResult.Fail("Token inválido.");
        var hash = SerynLinkService.Hash(raw);
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var token = await db.ApiTokens.SingleOrDefaultAsync(x => x.TokenHash == hash);
        if (token is null || token.RevokedAtUnix is not null || token.ExpiresAtUnix <= now)
            return AuthenticateResult.Fail("Token inválido ou expirado.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, token.Id.ToString()),
            new(ClaimTypes.Name, token.Name),
            new("seryn_link", token.Id.ToString())
        };
        claims.AddRange(token.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(scope => new Claim("scope", scope)));
        if (token.LastUsedAtUnix is null || token.LastUsedAtUnix < now - 3600)
        {
            token.LastUsedAtUnix = now;
            await db.SaveChangesAsync();
        }
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
