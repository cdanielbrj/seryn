using Seryn.Server.Infrastructure;
using Seryn.Server.Lumen;
using Seryn.Server.Modules.Auth;
using Seryn.Server.Modules.Classifications;
using Seryn.Server.Modules.Projections;
using Seryn.Server.Modules.Records;
using Seryn.Server.Modules.Objectives;
using Seryn.Server.Modules.Import;
using Seryn.Server.Modules.Export;

if (args.Contains("--healthcheck"))
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    try
    {
        return (await client.GetAsync("http://127.0.0.1:8080/health")).IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        return 1;
    }
    catch (TaskCanceledException)
    {
        return 1;
    }
}

var builder = WebApplication.CreateBuilder(args);

builder
    .AddSerynInfrastructure()
    .AddSerynSecurity();

builder.Services
    .AddAuthModule()
    .AddClassificationsModule()
    .AddRecordsModule()
    .AddObjectivesModule()
    .AddImportModule()
    .AddExportModule()
    .AddLumen()
    .AddProjectionsModule();

var app = builder.Build();
await app.InitializeSerynAsync();
app.UseSerynHttpPipeline();

var api = app.MapGroup("/api/v1").RequireAuthorization().RequireRateLimiting("api");
api.MapAuthModule();
api.MapClassificationsModule();
api.MapRecordsModule();
api.MapObjectivesModule();
api.MapImportModule();
api.MapExportModule();
api.MapLumen();
api.MapProjectionsModule();
api.MapGet("/foundation", () => new { phase = 2, financialDataAvailable = true }).WithTags("Fundação").RequireAuthorization("data:read")
    .WithName("GetFoundationStatus").WithSummary("Informa as capacidades fundamentais disponíveis");

app.MapSerynInfrastructure();
await app.RunAsync();
return 0;

public partial class Program;
