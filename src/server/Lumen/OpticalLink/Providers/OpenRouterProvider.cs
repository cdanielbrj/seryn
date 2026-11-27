using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Seryn.Server.Lumen.Core.Providers;
using Seryn.Server.Lumen.OpticalLink.Connections;

namespace Seryn.Server.Lumen.OpticalLink.Providers;

internal sealed class OpenRouterProvider(HttpClient client, OpticalConnectionService connections, ILogger<OpenRouterProvider> logger) : IModelProvider
{
    private ResolvedOpticalConnection? connection;

    public async Task<ModelReply> Complete(IReadOnlyList<ConversationMessage> messages, IReadOnlyList<ToolDefinition> tools, string? model, string sessionId, CancellationToken cancellationToken)
    {
        connection ??= await connections.Resolve(cancellationToken) ?? throw new ChatFailure(503, "Configure uma conexão padrão no Optical Link para conversar.");
        if (connection.Provider != "openrouter") throw new ChatFailure(502, "O provedor da conexão ainda não é compatível com esta versão.");
        var selectedModel = string.IsNullOrWhiteSpace(model) ? connection.Model : model;
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = selectedModel,
            session_id = sessionId,
            messages = messages.Select(Message),
            tools = tools.Select(tool => new { type = "function", function = new { name = tool.Name, description = tool.Description, parameters = tool.Parameters } }),
            tool_choice = "auto",
            max_tokens = 2048,
            stream = false,
            provider = new { require_parameters = true, data_collection = "deny" }
        });
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await ErrorDetails(response, cancellationToken);
            logger.LogWarning("OpenRouter request failed with status {StatusCode}, limit source {LimitSource}, request {RequestId}",
                (int)response.StatusCode, details.LimitSource ?? "unknown", details.RequestId ?? "unknown");
            throw (int)response.StatusCode switch
            {
                401 or 403 => new ChatFailure(502, "O provedor recusou a credencial ou o acesso ao modelo. Confira a configuração da conexão."),
                402 => new ChatFailure(502, "O provedor informou saldo insuficiente para este modelo."),
                429 => new ChatFailure(429, RateLimitMessage(details.LimitSource, response.Headers.RetryAfter)),
                400 or 404 or 422 => new ChatFailure(502, "O provedor não aceitou o modelo, as ferramentas ou a política de dados. Confira a configuração da conexão."),
                _ => new ChatFailure(502, "O provedor está indisponível. Tente novamente mais tarde.")
            };
        }

        // Bound the response before parsing; never log provider bodies or credentials.
        await response.Content.LoadIntoBufferAsync(256 * 1024, cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
            var root = document.RootElement;
            if (root.TryGetProperty("error", out _))
                throw new ChatFailure(502, "O provedor não conseguiu concluir a resposta.");
            var choice = root.GetProperty("choices")[0];
            if (choice.GetProperty("finish_reason").GetString() == "length")
                throw new ChatFailure(502, "A resposta atingiu o limite de tamanho. Tente uma pergunta mais específica.");
            var message = choice.GetProperty("message");
            var calls = new List<ToolRequest>();
            if (message.TryGetProperty("tool_calls", out var toolCalls))
                foreach (var call in toolCalls.EnumerateArray())
                {
                    var function = call.GetProperty("function");
                    calls.Add(new(call.GetProperty("id").GetString()!, function.GetProperty("name").GetString()!, function.GetProperty("arguments").GetString()!));
                }
            var text = message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String ? content.GetString() : null;
            var usage = root.TryGetProperty("usage", out var used) ? used : default;
            var responseModel = root.TryGetProperty("model", out var returnedModel) && returnedModel.ValueKind == JsonValueKind.String
                ? returnedModel.GetString()
                : null;
            return new(text, calls, TokenCount(usage, "prompt_tokens"), TokenCount(usage, "completion_tokens"),
                usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("cost", out var cost) && cost.TryGetDecimal(out var amount) ? amount : null,
                string.IsNullOrWhiteSpace(responseModel) ? selectedModel : responseModel);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            throw new ChatFailure(502, "O provedor retornou uma resposta inválida. Tente novamente.");
        }
    }

    private static long TokenCount(JsonElement usage, string name) =>
        usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty(name, out var value) && value.TryGetInt64(out var count) ? count : 0;

    private static async Task<(string? LimitSource, string? RequestId)> ErrorDetails(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string? limitSource = null;
        try
        {
            await response.Content.LoadIntoBufferAsync(64 * 1024, cancellationToken);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
            if (document.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("metadata", out var metadata) &&
                metadata.ValueKind == JsonValueKind.Object && metadata.TryGetProperty("limit_source", out var source) && source.ValueKind == JsonValueKind.String)
                limitSource = source.GetString();
        }
        catch (Exception exception) when (exception is JsonException or HttpRequestException or InvalidOperationException or IOException) { }
        var requestId = response.Headers.TryGetValues("x-request-id", out var values) ? values.FirstOrDefault() : null;
        return (Bound(limitSource, 80), Bound(requestId, 160));
    }

    private static string RateLimitMessage(string? source, RetryConditionHeaderValue? retryAfter)
    {
        var retry = retryAfter?.Delta is { } delay ? $" Aguarde cerca de {Math.Max(1, (int)Math.Ceiling(delay.TotalSeconds))} segundos." : "";
        if (source?.Contains("free_tier", StringComparison.OrdinalIgnoreCase) == true)
            return source.Contains("daily", StringComparison.OrdinalIgnoreCase)
                ? "A cota diária dos modelos gratuitos do OpenRouter foi atingida. Tente novamente após a renovação ou use outro modelo."
                : $"O limite temporário dos modelos gratuitos do OpenRouter foi atingido.{retry} Tente novamente ou use outro modelo.";
        if (source?.Contains("upstream", StringComparison.OrdinalIgnoreCase) == true)
            return "O modelo está temporariamente sem capacidade no provedor. Tente novamente ou selecione outro modelo.";
        return $"O limite do provedor foi atingido.{retry} Tente novamente ou selecione outro modelo.";
    }

    private static string? Bound(string? value, int maximum) => string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maximum)];

    private static Dictionary<string, object?> Message(ConversationMessage message)
    {
        var result = new Dictionary<string, object?> { ["role"] = message.Role, ["content"] = message.Content };
        if (message.ToolId is not null) result["tool_call_id"] = message.ToolId;
        if (message.Tools is { Count: > 0 })
            result["tool_calls"] = message.Tools.Select(tool => new { id = tool.Id, type = "function", function = new { name = tool.Name, arguments = tool.Arguments } });
        return result;
    }
}
