using Seryn.Server.Modules.Export.Application;

namespace Seryn.Server.Modules.Export;

public static class ExportModule
{
    public static IServiceCollection AddExportModule(this IServiceCollection services)
    {
        services.AddScoped<WorkbookExportService>();
        return services;
    }

    public static void MapExportModule(this RouteGroupBuilder api)
    {
        api.MapGet("/portability/export.xlsx", async (WorkbookExportService service) =>
                Results.File(await service.Export(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"seryn-dados-{DateTime.UtcNow:yyyyMMdd}.xlsx"))
            .RequireAuthorization("admin-session")
            .WithTags("Exportação")
            .WithName("ExportWorkbook")
            .WithSummary("Exporta os dados portáveis da instalação");
    }
}
