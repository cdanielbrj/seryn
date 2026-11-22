using Seryn.Server.Modules.Classifications.Application;
using Seryn.Server.Modules.Classifications.Categories;
using Seryn.Server.Modules.Classifications.Contracts;
using Seryn.Server.Modules.Classifications.PaymentMethods;
using Seryn.Server.Modules.Classifications.Types;

namespace Seryn.Server.Modules.Classifications;

public static class ClassificationsModule
{
    public static IServiceCollection AddClassificationsModule(this IServiceCollection services)
    {
        services.AddScoped<SimpleClassificationStore>();
        services.AddScoped<TypesHandler>();
        services.AddScoped<PaymentMethodsHandler>();
        services.AddScoped<CategoriesHandler>();
        return services;
    }

    public static void MapClassificationsModule(this RouteGroupBuilder api)
    {
        var types = api.MapGroup("/types").WithTags("Tipos");
        types.MapGet("/", (TypesHandler handler) => handler.List()).RequireAuthorization("data:read").WithName("ListTypes").WithSummary("Lista tipos");
        types.MapPost("/", async (ClassificationRequest request, TypesHandler handler) =>
            ToResult(await handler.Create(request.Nome), created: true)).RequireAuthorization("data:write").WithName("CreateType").WithSummary("Cria um tipo");
        types.MapPatch("/{id:guid}", async (Guid id, ClassificationRequest request, TypesHandler handler) =>
            ToResult(await handler.Update(id, request.Nome), created: false)).RequireAuthorization("data:write").WithName("UpdateType").WithSummary("Renomeia um tipo");
        types.MapDelete("/{id:guid}", async (Guid id, TypesHandler handler) => DeleteResult(await handler.Delete(id))).RequireAuthorization("data:delete").WithName("DeleteType").WithSummary("Exclui um tipo não utilizado");

        var paymentMethods = api.MapGroup("/payment-methods").WithTags("Pagamentos");
        paymentMethods.MapGet("/", (PaymentMethodsHandler handler) => handler.List()).RequireAuthorization("data:read").WithName("ListPaymentMethods").WithSummary("Lista meios de pagamento");
        paymentMethods.MapPost("/", async (ClassificationRequest request, PaymentMethodsHandler handler) =>
            ToResult(await handler.Create(request.Nome), created: true)).RequireAuthorization("data:write").WithName("CreatePaymentMethod").WithSummary("Cria um meio de pagamento");
        paymentMethods.MapPatch("/{id:guid}", async (Guid id, ClassificationRequest request, PaymentMethodsHandler handler) =>
            ToResult(await handler.Update(id, request.Nome), created: false)).RequireAuthorization("data:write").WithName("UpdatePaymentMethod").WithSummary("Renomeia um meio de pagamento");
        paymentMethods.MapDelete("/{id:guid}", async (Guid id, PaymentMethodsHandler handler) => DeleteResult(await handler.Delete(id))).RequireAuthorization("data:delete").WithName("DeletePaymentMethod").WithSummary("Exclui um meio de pagamento não utilizado");

        var categories = api.MapGroup("/categories").WithTags("Categorias");
        categories.MapGet("/", (CategoriesHandler handler) => handler.List()).RequireAuthorization("data:read").WithName("ListCategories").WithSummary("Lista categorias");
        categories.MapPost("/", async (CategoryRequest request, CategoriesHandler handler) =>
            ToResult(await handler.Create(request.Nome, request.CategoriaPaiId), created: true)).RequireAuthorization("data:write").WithName("CreateCategory").WithSummary("Cria uma categoria");
        categories.MapPatch("/{id:guid}", async (Guid id, CategoryRequest request, CategoriesHandler handler) =>
            ToResult(await handler.Update(id, request.Nome, request.CategoriaPaiId), created: false)).RequireAuthorization("data:write").WithName("UpdateCategory").WithSummary("Atualiza uma categoria");
        categories.MapDelete("/{id:guid}", async (Guid id, CategoriesHandler handler) => DeleteResult(await handler.Delete(id))).RequireAuthorization("data:delete").WithName("DeleteCategory").WithSummary("Exclui uma categoria conforme as regras de referência");
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
        ClassificationFailure.InUse => Results.Problem(statusCode: 409, title: "Esta classificação está sendo usada por um ou mais registros."),
        _ => Results.Problem(statusCode: 500, title: "Não foi possível concluir a operação.")
    };
}
