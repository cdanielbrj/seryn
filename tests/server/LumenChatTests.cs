using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Seryn.Server.Lumen.Core.Contracts;
using Seryn.Server.Lumen.Core.Conversations;
using Seryn.Server.Lumen.Core.Providers;
using Seryn.Server.Lumen.Core.Tools;
using Seryn.Server.Lumen.OpticalLink.Connections;
using Seryn.Server.Lumen.OpticalLink.Contracts;
using Seryn.Server.Lumen.OpticalLink.Providers;
using Seryn.Server.Lumen.Persona;

namespace Seryn.Tests;

public sealed class LumenChatTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-lumen-chat-test-" + Guid.NewGuid());

    [Fact]
    public async Task OpenRouter_provider_sends_tools_and_reports_the_model_that_answered()
    {
        await using var app = new TestApplication(directory);
        _ = app.CreateClient();
        using var scope = app.Services.CreateScope();
        var connections = scope.ServiceProvider.GetRequiredService<OpticalConnectionService>();
        var created = await connections.Create(new("Teste", "openrouter", "openrouter/free", "sk-or-v1-test-secret", true), CancellationToken.None);
        Assert.NotNull(created.Value);

        var handler = new RecordingHandler(new(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"model":"google/gemma-test:free","choices":[{"finish_reason":"stop","message":{"content":"OK"}}],"usage":{"prompt_tokens":12,"completion_tokens":3,"cost":0}}
                """, Encoding.UTF8, "application/json")
        });
        var provider = new OpenRouterProvider(new HttpClient(handler), connections, NullLogger<OpenRouterProvider>.Instance);
        var schema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement.Clone();

        var reply = await provider.Complete([new("user", "Responda OK")], [new("consultar", "Consulta dados", schema)], "anthropic/test-model", "5d26892e-5bd1-4d21-8d15-fbde519bd221", CancellationToken.None);

        Assert.Equal("OK", reply.Content);
        Assert.Equal("google/gemma-test:free", reply.Model);
        Assert.Equal(12, reply.InputTokens);
        Assert.Equal(3, reply.OutputTokens);
        Assert.Equal(0, reply.CostUsd);
        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal("sk-or-v1-test-secret", handler.Authorization?.Parameter);
        Assert.Equal("https://github.com/cdanielbrj/seryn", handler.ApplicationUrl);
        Assert.Equal("Seryn", handler.ApplicationTitle);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal("anthropic/test-model", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal("5d26892e-5bd1-4d21-8d15-fbde519bd221", payload.RootElement.GetProperty("session_id").GetString());
        Assert.Equal("consultar", payload.RootElement.GetProperty("tools")[0].GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("deny", payload.RootElement.GetProperty("provider").GetProperty("data_collection").GetString());
    }

    [Fact]
    public async Task OpenRouter_provider_distinguishes_an_upstream_capacity_limit_without_exposing_the_body()
    {
        await using var app = new TestApplication(directory);
        _ = app.CreateClient();
        using var scope = app.Services.CreateScope();
        var connections = scope.ServiceProvider.GetRequiredService<OpticalConnectionService>();
        await connections.Create(new("Teste", "openrouter", "modelo:free", "sk-or-v1-test-secret", true), CancellationToken.None);
        const string privateBody = "provider-private-diagnostic";
        var handler = new RecordingHandler(new(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("""
                {"error":{"message":"provider-private-diagnostic","metadata":{"limit_source":"upstream_provider_shared_pool"}}}
                """, Encoding.UTF8, "application/json")
        });
        var provider = new OpenRouterProvider(new HttpClient(handler), connections, NullLogger<OpenRouterProvider>.Instance);

        var failure = await Assert.ThrowsAsync<ChatFailure>(() => provider.Complete([new("user", "Teste")], [], null, "4daebc4b-a2bb-428b-b8ca-fc97a35674d6", CancellationToken.None));

        Assert.Equal(429, failure.Status);
        Assert.Contains("temporariamente sem capacidade", failure.Message);
        Assert.DoesNotContain(privateBody, failure.Message);
    }

    [Fact]
    public void Seryn_persona_is_host_specific_opinionated_and_grounded()
    {
        var message = new SerynPersona().SystemMessage("2026-11");

        Assert.Contains("You are Seryn", message);
        Assert.Contains("Respond in Brazilian Portuguese", message);
        Assert.Contains("current reference month is 2026-11", message);
        Assert.Contains("subtly opinionated", message);
        Assert.Contains("may judge choices", message);
        Assert.Contains("every clarification question required", message);
        Assert.Contains("values calculated by tools as exact", message);
        Assert.Contains("Do not invent data", message);
        Assert.Contains("returns text only", message);
        Assert.Contains("this version has none", message);
    }

    [Fact]
    public async Task Chat_service_completes_a_tool_round_before_returning_the_answer()
    {
        var provider = new ToolRoundProvider();
        var catalog = new ToolCatalog();
        using var gate = new ChatGate();
        var service = new ChatService(provider, catalog, new SerynPersona(), gate);

        var response = await service.Send(new([new("usuario", "Consulte meus dados")], "2026-11", "modelo-escolhido", "526cb631-8107-4c6d-ae4c-a547b7b9b25f"), CancellationToken.None);

        Assert.Equal("Resposta baseada na ferramenta.", response.Resposta);
        Assert.Equal("modelo-real", response.Modelo);
        Assert.Single(response.Consultas);
        Assert.Equal("consultar", response.Consultas[0].Ferramenta);
        Assert.Equal(15, response.Uso.EntradaTokens);
        Assert.Equal(6, response.Uso.SaidaTokens);
        Assert.Equal(0.02m, response.Uso.CustoUsd);
        Assert.Equal(2, provider.Calls);
        Assert.All(provider.RequestedModels, requested => Assert.Equal("modelo-escolhido", requested));
        Assert.All(provider.SessionIds, session => Assert.Equal("526cb631-8107-4c6d-ae4c-a547b7b9b25f", session));
        Assert.Contains(provider.SecondRound!, message => message.Role == "tool" && message.ToolId == "call-1" && message.Content!.Contains("resultado"));
    }

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }
        public string? ApplicationUrl { get; private set; }
        public string? ApplicationTitle { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            ApplicationUrl = request.Headers.TryGetValues("HTTP-Referer", out var urls) ? urls.SingleOrDefault() : null;
            ApplicationTitle = request.Headers.TryGetValues("X-OpenRouter-Title", out var titles) ? titles.SingleOrDefault() : null;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }

    private sealed class ToolRoundProvider : IModelProvider
    {
        public int Calls { get; private set; }
        public List<string?> RequestedModels { get; } = [];
        public List<string> SessionIds { get; } = [];
        public IReadOnlyList<ConversationMessage>? SecondRound { get; private set; }

        public Task<ModelReply> Complete(IReadOnlyList<ConversationMessage> messages, IReadOnlyList<ToolDefinition> tools, string? model, string sessionId, CancellationToken cancellationToken)
        {
            Calls++;
            RequestedModels.Add(model);
            SessionIds.Add(sessionId);
            if (Calls == 1)
                return Task.FromResult(new ModelReply(null, [new("call-1", "consultar", "{\"periodo\":\"mes\"}")], 10, 2, 0.01m, "modelo-real"));
            SecondRound = messages.ToList();
            return Task.FromResult(new ModelReply("Resposta baseada na ferramenta.", [], 5, 4, 0.01m, "modelo-real"));
        }
    }

    private sealed class ToolCatalog : ILumenToolCatalog
    {
        public IReadOnlyList<ToolDefinition> Definitions { get; } =
        [
            new("consultar", "Consulta", JsonDocument.Parse("""{"type":"object"}""").RootElement.Clone())
        ];

        public Task<ChatQuery> Execute(ToolRequest call, CancellationToken cancellationToken) => Task.FromResult(new ChatQuery(
            call.Name,
            JsonDocument.Parse(call.Arguments).RootElement.Clone(),
            JsonSerializer.SerializeToElement(new { resultado = 42 }),
            [new("Fonte", "/fonte")]));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
