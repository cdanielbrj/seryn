using Seryn.Server.Modules.ImportExport.Application;
using Seryn.Server.Modules.ImportExport.Contracts;

namespace Seryn.Server.Modules.ImportExport;

public static class ImportExportModule
{
    public static IServiceCollection AddImportExportModule(this IServiceCollection services) { services.AddScoped<WorkbookPortabilityService>(); return services; }
    public static void MapImportExportModule(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/portability").WithTags("Importação e exportação");
        group.MapGet("/export.xlsx", async (WorkbookPortabilityService service) => Results.File(await service.Export(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"seryn-dados-{DateTime.UtcNow:yyyyMMdd}.xlsx"));
        group.MapPost("/workbook/preview", async (WorkbookRequest request, WorkbookPortabilityService service) => Result(await service.Preview(request.ConteudoBase64)));
        group.MapPost("/workbook/commit", async (WorkbookCommitRequest request, WorkbookPortabilityService service) => Result(await service.Commit(request)));
    }
    private static IResult Result<T>((T? Value, Dictionary<string, string[]>? Errors) result) => result.Errors is null ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors);
}
