using System.Net;
using System.Net.Http.Json;
using System.Text;
using Seryn.Server.Modules.Auth.Contracts;
using Seryn.Server.Lumen.Core.Contracts;
using Seryn.Server.Lumen.OpticalLink.Contracts;

namespace Seryn.Tests;

public sealed class OpticalLinkTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-optical-link-test-" + Guid.NewGuid());
    private const string Password = "Uma frase de teste segura 123!";

    [Fact]
    public async Task Connection_is_protected_masked_default_and_readable_after_restart()
    {
        const string secret = "sk-or-v1-sensitive-test-secret-12345678";
        Guid id;
        await using (var app = new TestApplication(directory))
        {
            using var client = app.CreateClient();
            await Login(client);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/optical-link/connections")).StatusCode);
            var created = await Send(client, HttpMethod.Post, "/api/v1/lumen/optical-links",
                new { nome = "OpenRouter pessoal", provedor = "openrouter", modelo = "example/tool-model", chave = secret, padrao = false });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var item = (await created.Content.ReadFromJsonAsync<OpticalConnectionItem>())!;
            id = item.Id;
            Assert.True(item.Padrao);
            Assert.Equal("sk-o••••5678", item.Chave);
            Assert.DoesNotContain(secret, await created.Content.ReadAsStringAsync());
        }

        Assert.DoesNotContain(Directory.GetFiles(directory, "*", SearchOption.AllDirectories), file =>
            Encoding.UTF8.GetString(File.ReadAllBytes(file)).Contains(secret, StringComparison.Ordinal));

        await using var restarted = new TestApplication(directory);
        using var restartedClient = restarted.CreateClient();
        await Login(restartedClient);
        var status = await restartedClient.GetFromJsonAsync<ChatConnection>("/api/v1/lumen/chat/status");
        Assert.True(status!.Disponivel);
        Assert.Equal("example/tool-model", status.Modelo);
        var listed = (await restartedClient.GetFromJsonAsync<List<OpticalConnectionItem>>("/api/v1/lumen/optical-links"))!.Single();
        Assert.Equal(id, listed.Id);
        Assert.Equal("sk-o••••5678", listed.Chave);
    }

    private static async Task Login(HttpClient client) =>
        Assert.Equal(HttpStatusCode.OK, (await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "daniel", password = Password })).StatusCode);

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object data)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(data) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        return await client.SendAsync(request);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
