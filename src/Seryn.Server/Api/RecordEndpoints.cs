using Seryn.Server.Records;

namespace Seryn.Server.Api;

public static class RecordEndpoints
{
    public static void MapRecords(this RouteGroupBuilder api)
    {
        var records = api.MapGroup("/records").WithTags("Registros");
        records.MapGet("/", (RecordService service) => service.List());
        records.MapGet("/{id:guid}", async (Guid id, RecordService service) =>
            await service.Get(id) is { } item ? Results.Ok(item) : Results.Problem(statusCode: 404, title: "Registro não encontrado."));
        records.MapPost("/", async (RecordCommand command, RecordService service) =>
            ToResult(await service.Create(command), created: true));
        records.MapPatch("/{id:guid}", async (Guid id, RecordCommand command, RecordService service) =>
            ToResult(await service.Update(id, command), created: false));
        records.MapDelete("/{id:guid}", async (Guid id, RecordService service) =>
            await service.Delete(id) == RecordFailure.None
                ? Results.NoContent()
                : Results.Problem(statusCode: 404, title: "Registro não encontrado."));
    }

    private static IResult ToResult(RecordResult result, bool created) => result.Failure switch
    {
        RecordFailure.None when created => Results.Created($"./{result.Item!.Id}", result.Item),
        RecordFailure.None => Results.Ok(result.Item),
        RecordFailure.Invalid => Results.ValidationProblem(result.Errors!),
        RecordFailure.NotFound => Results.Problem(statusCode: 404, title: "Registro não encontrado."),
        _ => Results.Problem(statusCode: 500, title: "Não foi possível concluir a operação.")
    };
}
