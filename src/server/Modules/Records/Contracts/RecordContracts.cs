namespace Seryn.Server.Modules.Records.Contracts;

public sealed record RecordCommand(
    string? Nome,
    string? Movimentacao,
    Guid TipoId,
    Guid? CategoriaId,
    Guid PagamentoId,
    long ValorCentavos,
    string? Data,
    string? Recorrencia,
    string? Ocorrencia,
    int? Parcelas,
    string? ModoValor);

public sealed record RecordItem(
    Guid Id,
    string Nome,
    string Movimentacao,
    Guid TipoId,
    string TipoNome,
    Guid? CategoriaId,
    string? CategoriaNome,
    Guid PagamentoId,
    string PagamentoNome,
    long ValorCentavos,
    string Data,
    string Recorrencia,
    string Ocorrencia,
    int? Parcelas);

public enum RecordFailure
{
    None,
    Invalid,
    NotFound
}

public sealed record RecordResult(
    RecordItem? Item,
    RecordFailure Failure,
    Dictionary<string, string[]>? Errors = null);
