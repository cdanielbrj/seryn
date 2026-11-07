using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Persistence;

namespace Seryn.Server.Infrastructure;

public sealed record SerynPaths(string DataDirectory, DirectoryInfo KeysDirectory);

public static class SerynInfrastructure
{
    public static WebApplicationBuilder AddSerynInfrastructure(this WebApplicationBuilder builder)
    {
        var dataDirectory = Path.GetFullPath(builder.Configuration["Seryn:DataDirectory"] ?? "/data");
        Directory.CreateDirectory(dataDirectory);
        var keysDirectory = Directory.CreateDirectory(Path.Combine(dataDirectory, "keys"));
        RestrictDirectoryAccess(dataDirectory, keysDirectory.FullName);

        var paths = new SerynPaths(dataDirectory, keysDirectory);
        builder.Services.AddSingleton(paths);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddDbContext<SerynDbContext>(options => options.UseSqlite(
            new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(dataDirectory, "seryn.sqlite"),
                ForeignKeys = true,
                DefaultTimeout = 10
            }.ToString()));
        builder.Services.AddDataProtection()
            .SetApplicationName("Seryn")
            .PersistKeysToFileSystem(keysDirectory);
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi("v1");
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16 * 1024);
        return builder;
    }

    private static void RestrictDirectoryAccess(params string[] directories)
    {
        if (OperatingSystem.IsWindows())
            return;

        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        foreach (var directory in directories)
            File.SetUnixFileMode(directory, mode);
    }
}
