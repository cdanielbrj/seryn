using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Auth.Application;

// One process owns the SQLite database. Serialize login attempts to keep persistent
// lockout counters atomic, including concurrent requests.
public sealed class AuthGate : IDisposable
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public void Dispose() => Semaphore.Dispose();
}

public sealed class AuthService(SerynDbContext db, AuthGate gate,
    IPasswordHasher<LocalUser> hasher, TimeProvider clock)
{
    public async Task<LocalUser?> Login(string username, string password)
    {
        await gate.Semaphore.WaitAsync();
        try
        {
            var user = await db.Users.SingleOrDefaultAsync();
            if (user is null) return null;
            var now = clock.GetUtcNow().ToUnixTimeSeconds();
            if (user.LockedUntilUnix > now) return null;
            if (user.LockedUntilUnix is not null)
            {
                user.FailedAttempts = 0;
                user.LockedUntilUnix = null;
            }
            // Verify even for an incorrect name; errors never reveal which field failed.
            var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (!string.Equals(username.Trim(), user.Username, StringComparison.OrdinalIgnoreCase) ||
                verification == PasswordVerificationResult.Failed)
            {
                user.FailedAttempts++;
                if (user.FailedAttempts >= 5) user.LockedUntilUnix = now + 300;
                await db.SaveChangesAsync();
                return null;
            }
            user.FailedAttempts = 0;
            user.LockedUntilUnix = null;
            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = hasher.HashPassword(user, password);
            await db.SaveChangesAsync();
            return user;
        }
        finally { gate.Semaphore.Release(); }
    }

    public async Task<LoginSession> CreateSession()
    {
        var now = clock.GetUtcNow();
        await db.Sessions.Where(x => x.ExpiresAtUnix <= now.ToUnixTimeSeconds()).ExecuteDeleteAsync();
        var session = new LoginSession { Id = Guid.NewGuid().ToString("N"), ExpiresAtUnix = now.AddHours(8).ToUnixTimeSeconds() };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }
}
