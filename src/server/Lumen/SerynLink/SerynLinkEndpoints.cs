using Seryn.Server.Lumen.SerynLink.Contracts;

namespace Seryn.Server.Lumen.SerynLink;

internal static class SerynLinkEndpoints
{
    public static void MapSerynLink(this RouteGroupBuilder api)
    {
        var tokens = api.MapGroup("/seryn-links").RequireAuthorization("admin-session").WithTags("Lumen · Seryn Link");
        tokens.MapGet("/", (SerynLinkService service) => service.List())
            .WithName("ListSerynLinks").WithSummary("Lista Seryn Links ativos");
        tokens.MapPost("/", async (SerynLinkRequest request, SerynLinkService service) =>
        {
            var result = await service.Create(request);
            return result.Errors is null ? Results.Created($"./{result.Value!.Id}", result.Value) : Results.ValidationProblem(result.Errors);
        }).WithName("CreateSerynLink").WithSummary("Cria um Seryn Link")
            .WithDescription("O segredo é retornado uma única vez. Os escopos não podem ser alterados depois da criação.");
        tokens.MapDelete("/{id:guid}", async (Guid id, SerynLinkService service) =>
            await service.Revoke(id) ? Results.NoContent() : Results.Problem(statusCode: 404, title: "Seryn Link não encontrado."))
            .WithName("RevokeSerynLink").WithSummary("Revoga um Seryn Link");
    }
}
