using Seryn.Server.Modules.Import.Application;
using Seryn.Server.Modules.Import.Contracts;

namespace Seryn.Server.Modules.Import;

public static class ImportModule
{
    public static IServiceCollection AddImportModule(this IServiceCollection services)
    {
        services.AddScoped<WorkbookImportService>();
        return services;
    }

    public static void MapImportModule(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/portability/workbook").WithTags("Importação").RequireAuthorization("admin-session");
        group.MapPost("/preview", async (WorkbookRequest request, HttpContext context, WorkbookImportService service) =>
                Result(await service.Preview(request.ConteudoBase64, context.User.FindFirst("session")!.Value)))
            .WithName("PreviewWorkbookImport").WithSummary("Valida e pré-visualiza uma importação");
        group.MapPost("/commit", async (WorkbookCommitRequest request, HttpContext context, WorkbookImportService service) =>
                Result(await service.Commit(request, context.User.FindFirst("session")!.Value)))
            .WithName("CommitWorkbookImport").WithSummary("Confirma uma importação previamente revisada");
    }

    private static IResult Result<T>((T? Value, Dictionary<string, string[]>? Errors) result) =>
        result.Errors is null ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors);
}
