using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Lumen.SerynLink.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Lumen.SerynLink;

public sealed class SerynLinkService(SerynDbContext db, TimeProvider clock)
{
    private const string TokenPrefix = "seryn_pat_";

    public async Task<List<SerynLinkItem>> List()
    {
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        return await db.ApiTokens.AsNoTracking()
            .Where(x => x.RevokedAtUnix == null && (x.ExpiresAtUnix == null || x.ExpiresAtUnix > now))
            .OrderBy(x => x.Name).Select(x => ToItem(x)).ToListAsync();
    }

    public async Task<(SerynLinkCreated? Value, Dictionary<string, string[]>? Errors)> Create(SerynLinkRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var name = request.Nome?.Trim() ?? "";
        if (name.Length is < 1 or > 80) errors["nome"] = ["Informe um nome entre 1 e 80 caracteres."];
        var scopes = (request.Escopos ?? []).Distinct(StringComparer.Ordinal).Order().ToArray();
        if (scopes.Length == 0 || scopes.Except(SerynLinkScopes.All, StringComparer.Ordinal).Any())
            errors["escopos"] = ["Escolha ao menos um escopo válido: data:read, data:write ou data:delete."];
        if (scopes.Contains(SerynLinkScopes.Delete) && !scopes.Contains(SerynLinkScopes.Write))
            errors["escopos"] = ["data:delete requer também data:write."];
        long? expires = null;
        if (!string.IsNullOrWhiteSpace(request.ExpiraEm))
        {
            if (!DateTimeOffset.TryParseExact(request.ExpiraEm, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed) || parsed <= clock.GetUtcNow())
                errors["expiraEm"] = ["Informe uma data UTC futura no formato YYYY-MM-DDTHH:mm:ssZ."];
            else expires = parsed.ToUnixTimeSeconds();
        }
        if (errors.Count > 0) return (null, errors);
        var now = clock.GetUtcNow();
        if (await db.ApiTokens.CountAsync(x => x.RevokedAtUnix == null && (x.ExpiresAtUnix == null || x.ExpiresAtUnix > now.ToUnixTimeSeconds())) >= 20)
            return (null, new() { ["serynLinks"] = ["Revogue um Seryn Link antes de criar outro; o limite é 20 acessos ativos."] });

        var secret = TokenPrefix + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var entity = new ApiToken
        {
            Id = Guid.NewGuid(),
            Name = name,
            TokenHash = Hash(secret),
            DisplayPrefix = secret[..Math.Min(secret.Length, 21)],
            Scopes = string.Join(' ', scopes),
            CreatedAtUnix = now.ToUnixTimeSeconds(),
            ExpiresAtUnix = expires
        };
        db.ApiTokens.Add(entity);
        await db.SaveChangesAsync();
        return (new(entity.Id, entity.Name, secret, entity.DisplayPrefix, scopes, FormatRequired(entity.CreatedAtUnix), Format(entity.ExpiresAtUnix)), null);
    }

    public async Task<bool> Revoke(Guid id)
    {
        var token = await db.ApiTokens.SingleOrDefaultAsync(x => x.Id == id && x.RevokedAtUnix == null);
        if (token is null) return false;
        token.RevokedAtUnix = clock.GetUtcNow().ToUnixTimeSeconds();
        await db.SaveChangesAsync();
        return true;
    }

    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string[] ScopeArray(string scopes) => scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    private static SerynLinkItem ToItem(ApiToken x) => new(x.Id, x.Name, x.DisplayPrefix, ScopeArray(x.Scopes), FormatRequired(x.CreatedAtUnix), Format(x.ExpiresAtUnix), Format(x.LastUsedAtUnix));
    private static string FormatRequired(long value) => DateTimeOffset.FromUnixTimeSeconds(value).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    private static string? Format(long? value) => value is null ? null : DateTimeOffset.FromUnixTimeSeconds(value.Value).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
