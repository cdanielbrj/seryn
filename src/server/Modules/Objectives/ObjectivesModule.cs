using Seryn.Server.Modules.Objectives.Application;
using Seryn.Server.Modules.Objectives.Contracts;

namespace Seryn.Server.Modules.Objectives;

public static class ObjectivesModule
{
    public static IServiceCollection AddObjectivesModule(this IServiceCollection services) => services.AddScoped<ObjectiveService>();
    public static void MapObjectivesModule(this RouteGroupBuilder api)
    {
        var objectives = api.MapGroup("/objectives").WithTags("Objetivos");
        objectives.MapGet("/", (ObjectiveService service) => service.List());
        objectives.MapGet("/{id:guid}", async (Guid id, ObjectiveService service) => await service.Get(id) is { } item ? Results.Ok(item) : NotFound());
        objectives.MapPost("/", async (ObjectiveCommand command, ObjectiveService service) => Result(await service.Create(command), true));
        objectives.MapPatch("/{id:guid}", async (Guid id, ObjectiveCommand command, ObjectiveService service) => Result(await service.Update(id, command), false));
        objectives.MapDelete("/{id:guid}", async (Guid id, ObjectiveService service) => await service.Delete(id) == ObjectiveFailure.None ? Results.NoContent() : NotFound());
    }
    private static IResult Result(ObjectiveResult result, bool created) => result.Failure switch
    {
        ObjectiveFailure.None when created => Results.Created($"./{result.Item!.Id}", result.Item),
        ObjectiveFailure.None => Results.Ok(result.Item),
        ObjectiveFailure.Invalid => Results.ValidationProblem(result.Errors!),
        _ => NotFound()
    };
    private static IResult NotFound() => Results.Problem(statusCode: 404, title: "Objetivo não encontrado.");
}
