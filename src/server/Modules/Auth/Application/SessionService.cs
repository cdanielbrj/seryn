using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Auth.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Auth.Application;

public sealed class SessionService(SerynDbContext db, TimeProvider clock)
{
    public async Task<List<LoginSessionItem>> List(string currentId)
    {
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        await db.Sessions.Where(x => x.ExpiresAtUnix <= now).ExecuteDeleteAsync();
        return await db.Sessions.AsNoTracking().OrderByDescending(x => x.LastUsedAtUnix)
            .Select(x => new LoginSessionItem(x.Id, Format(x.CreatedAtUnix), Format(x.LastUsedAtUnix), Format(x.ExpiresAtUnix), x.Id == currentId))
            .ToListAsync();
    }

    public async Task<bool> Revoke(string id, string currentId)
    {
        if (string.Equals(id, currentId, StringComparison.Ordinal)) return false;
        return await db.Sessions.Where(x => x.Id == id).ExecuteDeleteAsync() > 0;
    }

    public Task<int> RevokeOthers(string currentId) => db.Sessions.Where(x => x.Id != currentId).ExecuteDeleteAsync();

    private static string Format(long value) => DateTimeOffset.FromUnixTimeSeconds(value).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
