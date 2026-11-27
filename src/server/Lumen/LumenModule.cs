using Seryn.Server.Lumen.Core.Contracts;
using Seryn.Server.Lumen.Core.Conversations;
using Seryn.Server.Lumen.Core.Providers;
using Seryn.Server.Lumen.Core.Tools;
using Seryn.Server.Lumen.OpticalLink.Connections;
using Seryn.Server.Lumen.OpticalLink.Contracts;
using Seryn.Server.Lumen.OpticalLink.Providers;
using Seryn.Server.Lumen.SerynLink;
using Seryn.Server.Lumen.Tools;

namespace Seryn.Server.Lumen;

public static class LumenModule
{
    public static IServiceCollection AddLumen(this IServiceCollection services)
    {
        services.AddSingleton<ChatGate>();
        services.AddScoped<OpticalConnectionService>();
        services.AddScoped<ChatService>();
        services.AddScoped<ILumenToolCatalog, SerynToolCatalog>();
        services.AddScoped<SerynLinkService>();
        services.AddHttpClient<IModelProvider, OpenRouterProvider>(client => client.Timeout = TimeSpan.FromSeconds(45))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .RemoveAllLoggers();
        return services;
    }

    public static void MapLumen(this RouteGroupBuilder api)
    {
        var lumen = api.MapGroup("/lumen");
        lumen.MapSerynLink();

        var chat = lumen.MapGroup("/chat").RequireAuthorization("admin-session").WithTags("Lumen · Conversa");
        chat.MapGet("/status", async (OpticalConnectionService service, CancellationToken cancellationToken) =>
        {
            var connection = await service.Resolve(cancellationToken);
            return connection is null
                ? new ChatConnection(false, "OpenRouter", null)
                : new ChatConnection(true, connection.Provider == "openrouter" ? "OpenRouter" : connection.Provider, connection.Model);
        })
            .WithName("GetChatConnection").WithSummary("Informa a disponibilidade da conexão de conversa sem expor credenciais");
        chat.MapPost("/", async (ChatRequest request, ChatService service, CancellationToken cancellationToken) =>
        {
            if (ChatService.Validate(request) is { } errors) return Results.ValidationProblem(errors);
            try { return Results.Ok(await service.Send(request, cancellationToken)); }
            catch (ChatFailure failure) { return Results.Problem(statusCode: failure.Status, title: failure.Message); }
        }).WithName("SendChatMessage").WithSummary("Conversa usando consultas de leitura sobre os dados da Seryn");

        var opticalLinks = lumen.MapGroup("/optical-links").RequireAuthorization("admin-session").WithTags("Lumen · Optical Link");
        opticalLinks.MapGet("/", (OpticalConnectionService service, CancellationToken cancellationToken) => service.List(cancellationToken))
            .WithName("ListOpticalLinkConnections").WithSummary("Lista conexões do Optical Link sem expor credenciais");
        opticalLinks.MapPost("/", async (OpticalConnectionRequest request, OpticalConnectionService service, CancellationToken cancellationToken) =>
        {
            var result = await service.Create(request, cancellationToken);
            return result.Errors is null ? Results.Created($"./{result.Value!.Id}", result.Value) : Results.ValidationProblem(result.Errors);
        }).WithName("CreateOpticalLinkConnection").WithSummary("Adiciona uma conexão criptografada ao Optical Link");
        opticalLinks.MapPut("/{id:guid}", async (Guid id, OpticalConnectionRequest request, OpticalConnectionService service, CancellationToken cancellationToken) =>
        {
            var result = await service.Update(id, request, cancellationToken);
            if (!result.Found) return Results.NotFound();
            return result.Errors is null ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors);
        }).WithName("UpdateOpticalLinkConnection").WithSummary("Atualiza uma conexão do Optical Link");
        opticalLinks.MapDelete("/{id:guid}", async (Guid id, OpticalConnectionService service, CancellationToken cancellationToken) =>
            await service.Delete(id, cancellationToken) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteOpticalLinkConnection").WithSummary("Remove uma conexão do Optical Link");
    }
}
