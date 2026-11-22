using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Persistence;
using Microsoft.OpenApi;

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
        builder.Services.AddSingleton<AsyncKeyedLock>();
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
        builder.Services.AddOpenApi("v1", options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Seryn API",
                Version = "v1",
                Description = "API versionada para dados de planejamento financeiro. Valores monetários usam centavos inteiros; projeções são expectativas e não saldos bancários. Autentique integrações com Authorization: Bearer <token>."
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["bearerToken"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "Seryn personal access token",
                Description = "Token pessoal revogável com escopos data:read, data:write e data:delete. Portabilidade e administração exigem sessão interativa."
            };
            return Task.CompletedTask;
        }));
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16 * 1024 * 1024);
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
