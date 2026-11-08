namespace Seryn.Server.Modules.Projections.Contracts;

public sealed record ProjectionQuery(string? Period, string? Reference);
public sealed record AnalysisQuery(string? Period, string? Reference, string? Direction, string? Primary, string? Secondary);
public sealed record ProjectionBucket(string Mes, long EntradasCentavos, long SaidasCentavos, long ResultadoAcumuladoCentavos, bool Selecionado);
public sealed record CategoryProjection(string Nome, long ValorCentavos, int Percentual);
public sealed record TopRecordProjection(string Id, string Nome, string? CategoriaNome, int Ocorrencias, long ValorCentavos);
public sealed record DashboardProjection(
    string Periodo,
    string Referencia,
    string Inicio,
    string Fim,
    long EntradasCentavos,
    long SaidasCentavos,
    long ResultadoCentavos,
    string GraficoInicio,
    string GraficoFim,
    List<ProjectionBucket> Grafico,
    List<CategoryProjection> Categorias,
    List<TopRecordProjection> MaioresEntradas,
    List<TopRecordProjection> MaioresSaidas);

public sealed record AnalysisRecordProjection(
    string Id,
    string Nome,
    string Movimentacao,
    string Tipo,
    string Categoria,
    string Pagamento,
    int Ocorrencias,
    int? ParcelasTotais,
    int? ParcelasPrevistasAteFim,
    long ValorCentavos);

public sealed record AnalysisCrossProjection(string Chave, string Nome, int Ocorrencias, int Registros, long ValorCentavos);
public sealed record AnalysisSegmentProjection(
    string Chave,
    string Nome,
    int Ocorrencias,
    int Registros,
    long ValorCentavos,
    int Percentual,
    List<AnalysisCrossProjection> Cruzamentos,
    List<AnalysisRecordProjection> Detalhes);

public sealed record AnalysisProjection(
    string Periodo,
    string Referencia,
    string Inicio,
    string Fim,
    string Direcao,
    string DimensaoPrincipal,
    string DimensaoSecundaria,
    long EntradasCentavos,
    long SaidasCentavos,
    long ResultadoCentavos,
    int Registros,
    int Ocorrencias,
    long BasePercentualCentavos,
    List<AnalysisSegmentProjection> Segmentos);
