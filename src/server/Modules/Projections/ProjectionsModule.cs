using Seryn.Server.Modules.Projections.Application;
using Seryn.Server.Modules.Projections.Contracts;

namespace Seryn.Server.Modules.Projections;

public static class ProjectionsModule
{
    public static IServiceCollection AddProjectionsModule(this IServiceCollection services)
    {
        services.AddScoped<DashboardProjectionService>();
        services.AddScoped<AnalysisProjectionService>();
        return services;
    }

    public static void MapProjectionsModule(this RouteGroupBuilder api)
    {
        api.MapGet("/projections", async ([AsParameters] ProjectionQuery query, DashboardProjectionService service) =>
        {
            var result = await service.Get(query);
            return result.Errors is null ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors);
        }).WithTags("Projeções").RequireAuthorization("data:read")
            .WithName("GetProjection").WithSummary("Calcula a projeção de um período")
            .WithDescription("Calcula ocorrências virtuais sem persistir parcelas. O resultado não representa saldo bancário.");
        api.MapGet("/analyses", async ([AsParameters] AnalysisQuery query, AnalysisProjectionService service) =>
        {
            var result = await service.Get(query);
            return result.Errors is null ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors);
        }).WithTags("Análises").RequireAuthorization("data:read")
            .WithName("GetAnalysis").WithSummary("Analisa a composição de valores projetados");
    }
}
