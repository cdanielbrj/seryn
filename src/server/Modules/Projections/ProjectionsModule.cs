using Seryn.Server.Modules.Projections.Application;
using Seryn.Server.Modules.Projections.Contracts;

namespace Seryn.Server.Modules.Projections;

public static class ProjectionsModule
{
    public static IServiceCollection AddProjectionsModule(this IServiceCollection services) =>
        services.AddScoped<DashboardProjectionService>();

    public static void MapProjectionsModule(this RouteGroupBuilder api) => api.MapGet("/projections", async ([AsParameters] ProjectionQuery query, DashboardProjectionService service) =>
    {
        var result = await service.Get(query);
        return result.Errors is null ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors);
    }).WithTags("Projeções");
}
