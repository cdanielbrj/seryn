namespace Seryn.Server.Modules.Objectives.Contracts;

public sealed record ObjectiveCommand(string? Nome, long ValorEstimadoCentavos, string? Interesse, int Prioridade, string? Link, string? Observacoes, string? DataDesejada);
public sealed record ObjectiveItem(Guid Id, string Nome, long ValorEstimadoCentavos, string Interesse, int Prioridade, string? Link, string? Observacoes, string? DataDesejada, string DataInclusao);
public sealed record ObjectiveResult(ObjectiveItem? Item, ObjectiveFailure Failure, Dictionary<string, string[]>? Errors = null);
public enum ObjectiveFailure { None, Invalid, NotFound }
