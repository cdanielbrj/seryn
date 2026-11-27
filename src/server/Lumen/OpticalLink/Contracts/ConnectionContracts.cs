namespace Seryn.Server.Lumen.OpticalLink.Contracts;

public sealed record OpticalConnectionRequest(string? Nome, string? Provedor, string? Modelo, string? Chave, bool Padrao);
public sealed record OpticalConnectionItem(Guid Id, string Nome, string Provedor, string Modelo, string Chave, bool Padrao, string CriadoEm, string AtualizadoEm);
