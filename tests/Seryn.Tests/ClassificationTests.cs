using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Seryn.Server.Api;
using Seryn.Server.Classifications;

namespace Seryn.Tests;

public sealed class ClassificationTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-classifications-" + Guid.NewGuid());
    private const string Password = "Uma frase de teste segura 123!";

    [Fact]
    public async Task Types_payment_methods_and_categories_have_authenticated_validated_crud_and_survive_restart()
    {
        await using (var app = new TestApplication(directory))
        {
            using var client = app.CreateClient();
            await Login(client);

            var created = await Send(client, HttpMethod.Post, "/api/v1/types", new { nome = "  Moradia  " });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var type = await created.Content.ReadFromJsonAsync<ClassificationItem>();
            Assert.Equal("Moradia", type!.Nome);

            var duplicate = await Send(client, HttpMethod.Post, "/api/v1/types", new { nome = "moradia" });
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            var invalid = await Send(client, HttpMethod.Post, "/api/v1/types", new { nome = "   " });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            var validation = await invalid.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.Contains("nome", validation!.Errors.Keys);

            var category = await Send(client, HttpMethod.Post, "/api/v1/categories", new { nome = "Moradia" });
            Assert.Equal(HttpStatusCode.Created, category.StatusCode);

            var payment = await Send(client, HttpMethod.Post, "/api/v1/payment-methods", new { nome = "  Pix  " });
            Assert.Equal(HttpStatusCode.Created, payment.StatusCode);
            var paymentMethod = await payment.Content.ReadFromJsonAsync<ClassificationItem>();
            Assert.Equal("Pix", paymentMethod!.Nome);
            Assert.Equal(HttpStatusCode.Conflict,
                (await Send(client, HttpMethod.Post, "/api/v1/payment-methods", new { nome = "pix" })).StatusCode);

            var updated = await Send(client, HttpMethod.Patch, $"/api/v1/types/{type.Id}", new { nome = "Despesa fixa" });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal("Despesa fixa", (await updated.Content.ReadFromJsonAsync<ClassificationItem>())!.Nome);
            Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Patch, $"/api/v1/types/{Guid.NewGuid()}", new { nome = "Outro" })).StatusCode);
        }

        await using (var restarted = new TestApplication(directory, password: null))
        {
            using var client = restarted.CreateClient();
            await Login(client);
            var types = await client.GetFromJsonAsync<List<ClassificationItem>>("/api/v1/types");
            var paymentMethods = await client.GetFromJsonAsync<List<ClassificationItem>>("/api/v1/payment-methods");
            var categories = await client.GetFromJsonAsync<List<ClassificationItem>>("/api/v1/categories");
            Assert.Single(types!);
            Assert.Equal("Despesa fixa", types![0].Nome);
            Assert.Single(paymentMethods!);
            Assert.Equal("Pix", paymentMethods![0].Nome);
            Assert.Single(categories!);

            Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Delete, $"/api/v1/types/{types[0].Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Delete, $"/api/v1/types/{types[0].Id}")).StatusCode);
            Assert.Empty((await client.GetFromJsonAsync<List<ClassificationItem>>("/api/v1/types"))!);
            Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Delete, $"/api/v1/payment-methods/{paymentMethods[0].Id}")).StatusCode);
        }
    }

    [Fact]
    public async Task Categories_support_one_level_scoped_names_moves_and_safe_parent_deletion()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        await Login(client);

        var software = await CreateCategory(client, "Software");
        var infrastructure = await CreateCategory(client, "Infraestrutura");
        var server = await CreateCategory(client, "Servidor", software.Id);
        var sameNameOtherParent = await CreateCategory(client, "Servidor", infrastructure.Id);
        Assert.NotEqual(server.Id, sameNameOtherParent.Id);

        Assert.Equal(HttpStatusCode.Conflict,
            (await Send(client, HttpMethod.Post, "/api/v1/categories", new { nome = "servidor", categoriaPaiId = software.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Send(client, HttpMethod.Post, "/api/v1/categories", new { nome = "Nível inválido", categoriaPaiId = server.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await Send(client, HttpMethod.Delete, $"/api/v1/categories/{software.Id}")).StatusCode);

        var moved = await Send(client, HttpMethod.Patch, $"/api/v1/categories/{server.Id}",
            new { nome = "API", categoriaPaiId = infrastructure.Id });
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(infrastructure.Id, (await moved.Content.ReadFromJsonAsync<CategoryItem>())!.CategoriaPaiId);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Send(client, HttpMethod.Delete, $"/api/v1/categories/{software.Id}")).StatusCode);

        var categories = (await client.GetFromJsonAsync<List<CategoryItem>>("/api/v1/categories"))!;
        Assert.DoesNotContain(categories, item => item.Id == software.Id);
        Assert.Equal(2, categories.Count(item => item.CategoriaPaiId == infrastructure.Id));
        Assert.DoesNotContain(categories, item => item.CategoriaPaiId is not null &&
            categories.Any(parent => parent.Id == item.CategoriaPaiId && parent.CategoriaPaiId is not null));
    }

    private static async Task<CategoryItem> CreateCategory(HttpClient client, string nome, Guid? categoriaPaiId = null)
    {
        var response = await Send(client, HttpMethod.Post, "/api/v1/categories", new { nome, categoriaPaiId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryItem>())!;
    }

    private static async Task Login(HttpClient client)
    {
        var response = await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "daniel", password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object? data = null)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        if (data is not null) request.Content = JsonContent.Create(data);
        return await client.SendAsync(request);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
