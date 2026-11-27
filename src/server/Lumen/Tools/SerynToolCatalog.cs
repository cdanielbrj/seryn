using System.Text.Json;
using Seryn.Server.Lumen.Core.Contracts;
using Seryn.Server.Lumen.Core.Providers;
using Seryn.Server.Lumen.Core.Tools;
using Seryn.Server.Modules.Projections.Application;
using Seryn.Server.Modules.Projections.Contracts;
using Seryn.Server.Modules.Records.GetRecord;
using Seryn.Server.Modules.Records.ListRecords;

namespace Seryn.Server.Lumen.Tools;

internal sealed class SerynToolCatalog(ListRecordsHandler records, GetRecordHandler record, DashboardProjectionService projections, AnalysisProjectionService analyses) : ILumenToolCatalog
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyList<ToolDefinition> ToolDefinitions =
    [
        Define("buscar_registros", "Busca registros originais por nome, tipo, categoria ou pagamento. Ordena pelo valor por ocorrência, não pelo impacto no período. Retorna até 20 itens; use deslocamento para continuar.", """
            {"type":"object","properties":{"busca":{"type":"string","maxLength":120},"direcao":{"type":"string","enum":["entrada","saida"]},"deslocamento":{"type":"integer","minimum":0,"maximum":1000}},"additionalProperties":false}
            """),
        Define("obter_registro", "Obtém os atributos originais de um registro pelo ID retornado pelas outras ferramentas.", """
            {"type":"object","properties":{"id":{"type":"string","format":"uuid"}},"required":["id"],"additionalProperties":false}
            """),
        Define("consultar_projecao", "Calcula entradas, saídas e resultado previstos e os maiores registros de um período civil. Valores em centavos; resultado não é saldo bancário.", """
            {"type":"object","properties":{"periodo":{"type":"string","enum":["mes","semestre","ano"]},"referencia":{"type":"string","pattern":"^[0-9]{4}-[0-9]{2}$"}},"required":["periodo","referencia"],"additionalProperties":false}
            """),
        Define("consultar_analises", "Analisa o impacto projetado por categoria, pagamento ou tipo. Distingue registros e ocorrências. Retorna totais completos, até 10 segmentos e até 5 detalhes por segmento; limites são informados. Nunca comprova pagamentos.", """
            {"type":"object","properties":{"periodo":{"type":"string","enum":["mes","semestre","ano"]},"referencia":{"type":"string","pattern":"^[0-9]{4}-[0-9]{2}$"},"direcao":{"type":"string","enum":["todas","entrada","saida"]},"dimensao":{"type":"string","enum":["categoria","pagamento","tipo"]},"cruzamento":{"type":"string","enum":["nenhum","categoria","pagamento","tipo"]}},"required":["periodo","referencia","direcao","dimensao"],"additionalProperties":false}
            """)
    ];

    public IReadOnlyList<ToolDefinition> Definitions => ToolDefinitions;

    public async Task<ChatQuery> Execute(ToolRequest call, CancellationToken cancellationToken)
    {
        var sources = new List<ChatSource>();
        JsonElement arguments = JsonSerializer.SerializeToElement(new { });
        object result;
        try
        {
            if (call.Arguments.Length > 2000) throw new ArgumentException();
            using var document = JsonDocument.Parse(call.Arguments);
            arguments = document.RootElement.Clone();
            if (arguments.ValueKind != JsonValueKind.Object) throw new ArgumentException();
            switch (call.Name)
            {
                case "buscar_registros":
                    Allow(arguments, "busca", "direcao", "deslocamento");
                    var search = Optional(arguments, "busca") ?? "";
                    var direction = Optional(arguments, "direcao");
                    var offset = arguments.TryGetProperty("deslocamento", out var skip) ? skip.GetInt32() : 0;
                    if (search.Length > 120 || direction is not (null or "entrada" or "saida") || offset is < 0 or > 1000) throw new ArgumentException();
                    var found = await records.Search(search, direction, offset, cancellationToken);
                    sources.AddRange(found.Items.Select(item => Source(item.Id.ToString(), item.Nome)));
                    result = new { registros = found.Items, haMais = found.HasMore, proximoDeslocamento = offset + found.Items.Count, criterio = "valor por ocorrência, decrescente" };
                    break;
                case "obter_registro":
                    Allow(arguments, "id");
                    if (!Guid.TryParse(Required(arguments, "id"), out var id)) throw new ArgumentException();
                    var item = await record.Handle(id);
                    if (item is not null) sources.Add(Source(item.Id.ToString(), item.Nome));
                    result = item is null ? new { erro = "Registro não encontrado." } : (object)item;
                    break;
                case "consultar_projecao":
                    Allow(arguments, "periodo", "referencia");
                    var projection = await projections.Get(new(Period(arguments), Required(arguments, "referencia")));
                    if (projection.Errors is not null) result = new { erros = projection.Errors };
                    else
                    {
                        var value = projection.Value!;
                        sources.Add(new("Projeção do período", "/"));
                        sources.AddRange(value.MaioresEntradas.Concat(value.MaioresSaidas).Select(row => Source(row.Id, row.Nome)));
                        result = new { value.Periodo, value.Referencia, value.Inicio, value.Fim, value.EntradasCentavos, value.SaidasCentavos, value.ResultadoCentavos, value.MaioresEntradas, value.MaioresSaidas };
                    }
                    break;
                case "consultar_analises":
                    Allow(arguments, "periodo", "referencia", "direcao", "dimensao", "cruzamento");
                    var analysis = await analyses.Get(new(Period(arguments), Required(arguments, "referencia"),
                        Required(arguments, "direcao") switch { "todas" => "all", "entrada" => "income", "saida" => "expense", _ => throw new ArgumentException() },
                        Dimension(Required(arguments, "dimensao")), Dimension(Optional(arguments, "cruzamento") ?? "nenhum")));
                    if (analysis.Errors is not null) result = new { erros = analysis.Errors };
                    else
                    {
                        var value = analysis.Value!;
                        sources.Add(new("Análises", "/analises"));
                        var segments = value.Segmentos.Take(10).Select(segment => new
                        {
                            segment.Nome,
                            segment.ValorCentavos,
                            segment.Percentual,
                            segment.Registros,
                            segment.Ocorrencias,
                            cruzamentos = segment.Cruzamentos.Take(5),
                            totalCruzamentos = segment.Cruzamentos.Count,
                            detalhes = segment.Detalhes.Take(5),
                            totalDetalhes = segment.Detalhes.Count
                        }).ToList();
                        sources.AddRange(segments.SelectMany(segment => segment.detalhes).Select(row => Source(row.Id, row.Nome)));
                        result = new
                        {
                            value.Periodo,
                            value.Referencia,
                            value.Inicio,
                            value.Fim,
                            value.Direcao,
                            value.DimensaoPrincipal,
                            value.DimensaoSecundaria,
                            value.EntradasCentavos,
                            value.SaidasCentavos,
                            value.ResultadoCentavos,
                            value.Registros,
                            value.Ocorrencias,
                            value.BasePercentualCentavos,
                            totalSegmentos = value.Segmentos.Count,
                            segmentos = segments
                        };
                    }
                    break;
                default:
                    result = new { erro = "Ferramenta indisponível. Use apenas as ferramentas de leitura fornecidas." };
                    break;
            }
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or FormatException or OverflowException)
        {
            result = new { erro = "Argumentos inválidos. Confira o esquema da ferramenta e solicite uma consulta válida." };
        }
        var data = JsonSerializer.SerializeToElement(result, Json);
        if (data.GetRawText().Length > 24000)
        {
            data = JsonSerializer.SerializeToElement(new { erro = "O resultado excede o limite. Refine a consulta ou consulte um registro pelo ID." });
            sources.Clear();
        }
        return new(call.Name, arguments, data, sources.Distinct().Take(30).ToList());
    }

    private static ToolDefinition Define(string name, string description, string schema) => new(name, description, JsonDocument.Parse(schema).RootElement.Clone());
    private static ChatSource Source(string id, string name) => new(name, $"/registros/{id}/editar");
    private static string Required(JsonElement arguments, string key) => Optional(arguments, key) ?? throw new ArgumentException();
    private static string? Optional(JsonElement arguments, string key) => arguments.TryGetProperty(key, out var value) ? value.GetString() : null;
    private static void Allow(JsonElement arguments, params string[] keys)
    {
        if (arguments.EnumerateObject().Any(property => !keys.Contains(property.Name))) throw new ArgumentException();
    }
    private static string Period(JsonElement arguments) => Required(arguments, "periodo") switch
    {
        "mes" => "month",
        "semestre" => "semester",
        "ano" => "year",
        _ => throw new ArgumentException()
    };
    private static string Dimension(string value) => value switch
    {
        "categoria" => "category",
        "pagamento" => "payment",
        "tipo" => "type",
        "nenhum" => "none",
        _ => throw new ArgumentException()
    };
}
