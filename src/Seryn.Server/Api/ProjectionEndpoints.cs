using Seryn.Server.Projections;

namespace Seryn.Server.Api;

public static class ProjectionEndpoints
{
    public static void MapProjections(this RouteGroupBuilder api) => api.MapGet("/projections", async ([AsParameters] ProjectionQuery query, ProjectionService service) =>
    {
        var result = await service.Get(query);
        return result.Errors is null ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors);
    }).WithTags("Projeções");
}
