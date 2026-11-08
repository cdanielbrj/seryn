using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Seryn.Server.Modules.Auth.Contracts;
using Seryn.Server.Modules.Objectives.Contracts;

namespace Seryn.Tests;

public sealed class ObjectiveTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-objectives-" + Guid.NewGuid());
    private const string Password = "Uma frase de teste segura 123!";

    [Fact]
    public async Task Objectives_have_validated_crud_default_order_and_survive_restart()
    {
        Guid listedId;
        await using (var app = new TestApplication(directory))
        {
            using var client = app.CreateClient();
            await Login(client);
            var listed = await Create(client, new { nome = "Notebook", valorEstimadoCentavos = 600000, interesse = "listado", prioridade = 3, link = "https://example.com/notebook", observacoes = "Comparar memória", dataDesejada = "2027-03" });
            listedId = listed.Id;
            Assert.Equal("2027-03", listed.DataDesejada);
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", listed.DataInclusao);
            var desired = await Create(client, new { nome = "Cadeira", valorEstimadoCentavos = 250000, interesse = "desejado", prioridade = -2, link = (string?)null, observacoes = (string?)null, dataDesejada = (string?)null });
            var ordered = (await client.GetFromJsonAsync<List<ObjectiveItem>>("/api/v1/objectives"))!;
            Assert.Equal([desired.Id, listed.Id], ordered.Select(x => x.Id));

            var updatedResponse = await Send(client, HttpMethod.Patch, $"/api/v1/objectives/{listed.Id}", new { nome = "Notebook Pro", valorEstimadoCentavos = 700000, interesse = "considerado", prioridade = 1, link = "", observacoes = "", dataDesejada = "" });
            Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
            var updated = (await updatedResponse.Content.ReadFromJsonAsync<ObjectiveItem>())!;
            Assert.Equal("Notebook Pro", updated.Nome);
            Assert.Null(updated.Link);
            Assert.Equal(listed.DataInclusao, updated.DataInclusao);
        }
        await using var restarted = new TestApplication(directory, password: null);
        using var restartedClient = restarted.CreateClient();
        await Login(restartedClient);
        Assert.NotNull(await restartedClient.GetFromJsonAsync<ObjectiveItem>($"/api/v1/objectives/{listedId}"));
        Assert.Equal(HttpStatusCode.NoContent, (await Send(restartedClient, HttpMethod.Delete, $"/api/v1/objectives/{listedId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await restartedClient.GetAsync($"/api/v1/objectives/{listedId}")).StatusCode);
    }

    [Fact]
    public async Task Objective_validation_rejects_invalid_values()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        await Login(client);
        var response = await Send(client, HttpMethod.Post, "/api/v1/objectives", new { nome = " ", valorEstimadoCentavos = 0, interesse = "urgente", prioridade = 4, link = "javascript:alert(1)", observacoes = new string('x', 2001), dataDesejada = "2027-13" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        foreach (var field in new[] { "nome", "valorEstimadoCentavos", "interesse", "prioridade", "link", "observacoes", "dataDesejada" }) Assert.Contains(field, problem.Errors.Keys);
    }

    private static async Task<ObjectiveItem> Create(HttpClient client, object value) { var response = await Send(client, HttpMethod.Post, "/api/v1/objectives", value); Assert.Equal(HttpStatusCode.Created, response.StatusCode); return (await response.Content.ReadFromJsonAsync<ObjectiveItem>())!; }
    private static async Task Login(HttpClient client) { var response = await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "daniel", password = Password }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }
    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object? data = null) { var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf"); using var request = new HttpRequestMessage(method, path); request.Headers.Add("X-CSRF-TOKEN", csrf!.Token); if (data is not null) request.Content = JsonContent.Create(data); return await client.SendAsync(request); }
    public void Dispose() { SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
