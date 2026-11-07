namespace Seryn.Server.Modules.Projections.Contracts;

public sealed record ProjectionQuery(string? Period, string? Reference);
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
