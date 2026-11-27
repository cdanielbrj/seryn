using System.Text.Json;

namespace Seryn.Server.Lumen.Core.Providers;

internal sealed record ToolDefinition(string Name, string Description, JsonElement Parameters);
internal sealed record ToolRequest(string Id, string Name, string Arguments);
internal sealed record ConversationMessage(string Role, string? Content, IReadOnlyList<ToolRequest>? Tools = null, string? ToolId = null);
internal sealed record ModelReply(string? Content, List<ToolRequest> Tools, long InputTokens, long OutputTokens, decimal? CostUsd, string Model);

internal interface IModelProvider
{
    Task<ModelReply> Complete(IReadOnlyList<ConversationMessage> messages, IReadOnlyList<ToolDefinition> tools, string? model, string sessionId, CancellationToken cancellationToken);
}

internal sealed class ChatFailure(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
