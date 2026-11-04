using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Seryn.Server.Api;
using Seryn.Server.Classifications;
using Seryn.Server.Records;

namespace Seryn.Tests;

public sealed class RecordTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-records-" + Guid.NewGuid());
    private const string Password = "Uma frase de teste segura 123!";

    [Fact]
    public async Task Records_have_canonical_crud_references_and_survive_restart()
    {
        Guid firstId;
        Guid typeId;
        Guid paymentId;
        Guid rootId;
        await using (var app = new TestApplication(directory))
        {
            using var client = app.CreateClient();
            await Login(client);
            typeId = (await CreateClassification(client, "/api/v1/types", "Compra")).Id;
            paymentId = (await CreateClassification(client, "/api/v1/payment-methods", "Pix")).Id;
            rootId = (await CreateCategory(client, "Software")).Id;
            var childId = (await CreateCategory(client, "Servidor", rootId)).Id;

            var unique = await Send(client, HttpMethod.Post, "/api/v1/records", new
            {
                nome = "  API anual  ",
                movimentacao = "saida",
                tipoId = typeId,
                categoriaId = childId,
                pagamentoId = paymentId,
                valorCentavos = 75000,
                data = "2027-01",
                recorrencia = "unica",
                ocorrencia = "indefinida",
                parcelas = 99
            });
            Assert.Equal(HttpStatusCode.Created, unique.StatusCode);
            var item = (await unique.Content.ReadFromJsonAsync<RecordItem>())!;
            firstId = item.Id;
            Assert.Equal("API anual", item.Nome);
            Assert.Equal("definida", item.Ocorrencia);
            Assert.Equal(1, item.Parcelas);
            Assert.Equal("Servidor", item.CategoriaNome);

            var recurring = await Send(client, HttpMethod.Post, "/api/v1/records", new
            {
                nome = "Contrato",
                movimentacao = "entrada",
                tipoId = typeId,
                categoriaId = (Guid?)null,
                pagamentoId = paymentId,
                valorCentavos = 10000,
                data = "2027-02",
                recorrencia = "trimestral",
                ocorrencia = "definida",
                parcelas = 3,
                modoValor = "total"
            });
            Assert.Equal(HttpStatusCode.Created, recurring.StatusCode);
            var recurringItem = (await recurring.Content.ReadFromJsonAsync<RecordItem>())!;
            Assert.Equal(3, recurringItem.Parcelas);
            Assert.Equal(3333, recurringItem.ValorCentavos);

            var updated = await Send(client, HttpMethod.Patch, $"/api/v1/records/{recurringItem.Id}", new
            {
                nome = "Contrato contínuo",
                movimentacao = "entrada",
                tipoId = typeId,
                categoriaId = rootId,
                pagamentoId = paymentId,
                valorCentavos = 310000,
                data = "2027-03",
                recorrencia = "anual",
                ocorrencia = "indefinida",
                parcelas = (int?)null
            });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            var updatedItem = (await updated.Content.ReadFromJsonAsync<RecordItem>())!;
            Assert.Null(updatedItem.Parcelas);
            Assert.Equal("indefinida", updatedItem.Ocorrencia);

            Assert.Equal(HttpStatusCode.Conflict, (await Send(client, HttpMethod.Delete, $"/api/v1/types/{typeId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await Send(client, HttpMethod.Delete, $"/api/v1/payment-methods/{paymentId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Delete, $"/api/v1/categories/{childId}")).StatusCode);
            Assert.Equal(rootId, (await client.GetFromJsonAsync<RecordItem>($"/api/v1/records/{firstId}"))!.CategoriaId);
        }

        await using (var restarted = new TestApplication(directory, password: null))
        {
            using var client = restarted.CreateClient();
            await Login(client);
            var records = (await client.GetFromJsonAsync<List<RecordItem>>("/api/v1/records"))!;
            Assert.Equal(2, records.Count);
            Assert.Equal(2, records.Select(x => x.Id).Distinct().Count());
            Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Delete, $"/api/v1/records/{firstId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Delete, $"/api/v1/records/{firstId}")).StatusCode);
        }
    }

    [Fact]
    public async Task Record_validation_rejects_incomplete_or_incoherent_input()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        await Login(client);

        var response = await Send(client, HttpMethod.Post, "/api/v1/records", new
        {
            nome = " ",
            movimentacao = "transferencia",
            tipoId = Guid.NewGuid(),
            categoriaId = Guid.NewGuid(),
            pagamentoId = Guid.NewGuid(),
            valorCentavos = 0,
            data = "2027-13",
            recorrencia = "mensal",
            ocorrencia = "definida",
            parcelas = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        foreach (var field in new[] { "nome", "movimentacao", "tipoId", "categoriaId", "pagamentoId", "valorCentavos", "data", "parcelas" })
            Assert.Contains(field, problem.Errors.Keys);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/records/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await Send(client, HttpMethod.Patch, $"/api/v1/records/{Guid.NewGuid()}", new { })).StatusCode);
    }

    private static async Task<ClassificationItem> CreateClassification(HttpClient client, string path, string nome)
    {
        var response = await Send(client, HttpMethod.Post, path, new { nome });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClassificationItem>())!;
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
