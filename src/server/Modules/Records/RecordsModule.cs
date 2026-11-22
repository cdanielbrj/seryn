using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Modules.Records.Application;
using Seryn.Server.Modules.Records.CreateRecord;
using Seryn.Server.Modules.Records.DeleteRecord;
using Seryn.Server.Modules.Records.GetRecord;
using Seryn.Server.Modules.Records.ListRecords;
using Seryn.Server.Modules.Records.UpdateRecord;

namespace Seryn.Server.Modules.Records;

public static class RecordsModule
{
    public static IServiceCollection AddRecordsModule(this IServiceCollection services)
    {
        services.AddScoped<RecordCommandProcessor>();
        services.AddScoped<ListRecordsHandler>();
        services.AddScoped<GetRecordHandler>();
        services.AddScoped<CreateRecordHandler>();
        services.AddScoped<UpdateRecordHandler>();
        services.AddScoped<DeleteRecordHandler>();
        return services;
    }

    public static void MapRecordsModule(this RouteGroupBuilder api)
    {
        var records = api.MapGroup("/records").WithTags("Registros");
        records.MapGet("/", (ListRecordsHandler handler) => handler.Handle()).RequireAuthorization("data:read")
            .WithName("ListRecords").WithSummary("Lista registros financeiros originais");
        records.MapGet("/{id:guid}", async (Guid id, GetRecordHandler handler) =>
            await handler.Handle(id) is { } item ? Results.Ok(item) : Results.Problem(statusCode: 404, title: "Registro não encontrado.")).RequireAuthorization("data:read")
            .WithName("GetRecord").WithSummary("Obtém um registro financeiro original");
        records.MapPost("/", async (RecordCommand command, CreateRecordHandler handler) =>
            ToResult(await handler.Handle(command), created: true)).RequireAuthorization("data:write")
            .WithName("CreateRecord").WithSummary("Cria um registro financeiro")
            .WithDescription("O valor é positivo e por ocorrência. Use Idempotency-Key em integrações para tornar repetições seguras por 24 horas.");
        records.MapPatch("/{id:guid}", async (Guid id, RecordCommand command, UpdateRecordHandler handler) =>
            ToResult(await handler.Handle(id, command), created: false)).RequireAuthorization("data:write")
            .WithName("UpdateRecord").WithSummary("Substitui os campos editáveis de um registro");
        records.MapDelete("/{id:guid}", async (Guid id, DeleteRecordHandler handler) =>
            await handler.Handle(id) == RecordFailure.None
                ? Results.NoContent()
                : Results.Problem(statusCode: 404, title: "Registro não encontrado.")).RequireAuthorization("data:delete")
            .WithName("DeleteRecord").WithSummary("Exclui um registro financeiro original");
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
