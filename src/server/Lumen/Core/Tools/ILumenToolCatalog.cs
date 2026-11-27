using Seryn.Server.Lumen.Core.Contracts;
using Seryn.Server.Lumen.Core.Providers;

namespace Seryn.Server.Lumen.Core.Tools;

internal interface ILumenToolCatalog
{
    IReadOnlyList<ToolDefinition> Definitions { get; }
    Task<ChatQuery> Execute(ToolRequest call, CancellationToken cancellationToken);
}
