using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Seryn.Server.Modules.Auth.Contracts;
using Seryn.Server.Modules.Classifications.Contracts;
using Seryn.Server.Modules.Records.Contracts;

namespace Seryn.Tests;

public sealed class ApiIntegrationTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-api-test-" + Guid.NewGuid());
    private const string Password = "Uma frase de teste segura 123!";

    [Fact]
    public async Task Read_token_can_query_data_but_cannot_mutate_or_use_portability()
    {
        await using var app = new TestApplication(directory);
        using var admin = app.CreateClient();
        await Login(admin);
        var created = await CreateToken(admin, "Leitura", ["data:read"]);
        using var client = TokenClient(app, created.Token);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/records")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/types", new { nome = "Compra" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/portability/export.xlsx")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/tokens")).StatusCode);
        var openApi = await client.GetStringAsync("/api/v1/openapi/v1.json");
        Assert.Contains("bearerToken", openApi);
        Assert.Contains("ListRecords", openApi);
        Assert.Contains("Idempotency-Key", openApi, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Write_and_delete_scopes_follow_business_rules_and_revoke_immediately()
    {
        await using var app = new TestApplication(directory);
        using var admin = app.CreateClient();
        await Login(admin);
        var created = await CreateToken(admin, "Automação", ["data:read", "data:write", "data:delete"]);
        using var client = TokenClient(app, created.Token);

        var typeResponse = await client.PostAsJsonAsync("/api/v1/types", new { nome = "Compra" });
        Assert.Equal(HttpStatusCode.Created, typeResponse.StatusCode);
        var type = (await typeResponse.Content.ReadFromJsonAsync<ClassificationItem>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/types/{type.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await SendAdmin(admin, HttpMethod.Delete, $"/api/v1/auth/tokens/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/records")).StatusCode);
    }

    [Fact]
    public async Task Idempotency_replays_creation_and_rejects_key_reuse_with_another_payload()
    {
        await using var app = new TestApplication(directory);
        using var admin = app.CreateClient();
        await Login(admin);
        var token = await CreateToken(admin, "Gravação", ["data:read", "data:write"]);
        using var client = TokenClient(app, token.Token);
        var type = await CreateClassification(client, "/api/v1/types", "Compra");
        var payment = await CreateClassification(client, "/api/v1/payment-methods", "Pix");
        var payload = new { nome = "Mesa", movimentacao = "saida", tipoId = type.Id, pagamentoId = payment.Id, valorCentavos = 10000, data = "2027-01", recorrencia = "unica", ocorrencia = "definida", parcelas = 1 };

        var requests = await Task.WhenAll(
            IdempotentPost(client, "/api/v1/records", "record-create-123", payload),
            IdempotentPost(client, "/api/v1/records", "record-create-123", payload));
        var first = requests.Single(response => !response.Headers.Contains("Idempotency-Replayed"));
        var replay = requests.Single(response => response.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.True(replay.Headers.TryGetValues("Idempotency-Replayed", out var values) && values.Single() == "true");
        Assert.Equal((await first.Content.ReadFromJsonAsync<RecordItem>())!.Id, (await replay.Content.ReadFromJsonAsync<RecordItem>())!.Id);
        Assert.Single((await client.GetFromJsonAsync<List<RecordItem>>("/api/v1/records"))!);

        var conflict = await IdempotentPost(client, "/api/v1/records", "record-create-123", new { nome = "Outro" });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Token_validation_rejects_invalid_scopes_and_delete_without_write()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        await Login(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAdmin(client, HttpMethod.Post, "/api/v1/auth/tokens", new { nome = "Inválido", escopos = new[] { "admin" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAdmin(client, HttpMethod.Post, "/api/v1/auth/tokens", new { nome = "Inválido", escopos = new[] { "data:delete" } })).StatusCode);
    }

    private static HttpClient TokenClient(TestApplication app, string token)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<ApiTokenCreated> CreateToken(HttpClient client, string name, string[] scopes)
    {
        var response = await SendAdmin(client, HttpMethod.Post, "/api/v1/auth/tokens", new { nome = name, escopos = scopes });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiTokenCreated>())!;
    }

    private static async Task<ClassificationItem> CreateClassification(HttpClient client, string path, string name)
    {
        var response = await client.PostAsJsonAsync(path, new { nome = name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClassificationItem>())!;
    }

    private static async Task<HttpResponseMessage> IdempotentPost(HttpClient client, string path, string key, object payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    private static async Task Login(HttpClient client) =>
        Assert.Equal(HttpStatusCode.OK, (await SendAdmin(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "daniel", password = Password })).StatusCode);

    private static async Task<HttpResponseMessage> SendAdmin(HttpClient client, HttpMethod method, string path, object? data = null)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(method, path);
        if (data is not null) request.Content = JsonContent.Create(data);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        return await client.SendAsync(request);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
        GC.SuppressFinalize(this);
    }
}
