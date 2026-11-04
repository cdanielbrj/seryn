using Seryn.Server.Classifications;

namespace Seryn.Server.Api;

public sealed record ClassificationRequest(string? Nome);
public sealed record CategoryRequest(string? Nome, Guid? CategoriaPaiId);

public static class ClassificationEndpoints
{
    public static void MapClassifications(this RouteGroupBuilder api)
    {
        var types = api.MapGroup("/types").WithTags("Tipos");
        types.MapGet("/", (ClassificationService service) => service.ListTypes());
        types.MapPost("/", async (ClassificationRequest request, ClassificationService service) =>
            ToResult(await service.CreateType(request.Nome), created: true));
        types.MapPatch("/{id:guid}", async (Guid id, ClassificationRequest request, ClassificationService service) =>
            ToResult(await service.UpdateType(id, request.Nome), created: false));
        types.MapDelete("/{id:guid}", async (Guid id, ClassificationService service) => DeleteResult(await service.DeleteType(id)));

        var paymentMethods = api.MapGroup("/payment-methods").WithTags("Pagamentos");
        paymentMethods.MapGet("/", (ClassificationService service) => service.ListPaymentMethods());
        paymentMethods.MapPost("/", async (ClassificationRequest request, ClassificationService service) =>
            ToResult(await service.CreatePaymentMethod(request.Nome), created: true));
        paymentMethods.MapPatch("/{id:guid}", async (Guid id, ClassificationRequest request, ClassificationService service) =>
            ToResult(await service.UpdatePaymentMethod(id, request.Nome), created: false));
        paymentMethods.MapDelete("/{id:guid}", async (Guid id, ClassificationService service) => DeleteResult(await service.DeletePaymentMethod(id)));

        var categories = api.MapGroup("/categories").WithTags("Categorias");
        categories.MapGet("/", (ClassificationService service) => service.ListCategories());
        categories.MapPost("/", async (CategoryRequest request, ClassificationService service) =>
            ToResult(await service.CreateCategory(request.Nome, request.CategoriaPaiId), created: true));
        categories.MapPatch("/{id:guid}", async (Guid id, CategoryRequest request, ClassificationService service) =>
            ToResult(await service.UpdateCategory(id, request.Nome, request.CategoriaPaiId), created: false));
        categories.MapDelete("/{id:guid}", async (Guid id, ClassificationService service) => DeleteResult(await service.DeleteCategory(id)));
    }

    private static IResult ToResult<T>(ClassificationResult<T> result, bool created) where T : class => result.Failure switch
    {
        ClassificationFailure.None when created => Results.Created($"./{GetId(result.Item!)}", result.Item),
        ClassificationFailure.None => Results.Ok(result.Item),
        ClassificationFailure.InvalidName => Results.ValidationProblem(new Dictionary<string, string[]>
            { ["nome"] = ["Informe um nome entre 1 e 80 caracteres."] }),
        ClassificationFailure.Duplicate => Results.Problem(statusCode: 409, title: "Já existe uma classificação com esse nome neste nível."),
        ClassificationFailure.NotFound => Results.Problem(statusCode: 404, title: "Classificação não encontrada."),
        ClassificationFailure.InvalidParent => Results.ValidationProblem(new Dictionary<string, string[]>
            { ["categoriaPaiId"] = ["Escolha uma categoria principal válida."] }),
        _ => Results.Problem(statusCode: 500, title: "Não foi possível concluir a operação.")
    };

    private static Guid GetId<T>(T item) => item switch
    {
        ClassificationItem classification => classification.Id,
        CategoryItem category => category.Id,
        _ => throw new ArgumentOutOfRangeException(nameof(item))
    };

    private static IResult DeleteResult(ClassificationFailure failure) => failure switch
    {
        ClassificationFailure.None => Results.NoContent(),
        ClassificationFailure.NotFound => Results.Problem(statusCode: 404, title: "Classificação não encontrada."),
        ClassificationFailure.HasChildren => Results.Problem(statusCode: 409, title: "Mova ou exclua as subcategorias antes de excluir esta categoria."),
        _ => Results.Problem(statusCode: 500, title: "Não foi possível concluir a operação.")
    };
}
