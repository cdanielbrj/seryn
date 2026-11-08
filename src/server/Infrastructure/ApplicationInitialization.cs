using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Auth.Application;
using Seryn.Server.Persistence;

namespace Seryn.Server.Infrastructure;

public static class ApplicationInitialization
{
    public static async Task InitializeSerynAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SerynDbContext>();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await scope.ServiceProvider.GetRequiredService<AdminProvisioner>().Provision();

        var paths = scope.ServiceProvider.GetRequiredService<SerynPaths>();
        File.Delete(Path.Combine(paths.DataDirectory, "bootstrap.token"));
    }
}
