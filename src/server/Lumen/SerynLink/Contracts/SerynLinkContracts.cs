namespace Seryn.Server.Lumen.SerynLink.Contracts;

public sealed record SerynLinkRequest(string? Nome, string[]? Escopos, string? ExpiraEm);
public sealed record SerynLinkItem(Guid Id, string Nome, string Prefixo, string[] Escopos, string CriadoEm, string? ExpiraEm, string? UltimoUsoEm);
public sealed record SerynLinkCreated(Guid Id, string Nome, string Token, string Prefixo, string[] Escopos, string CriadoEm, string? ExpiraEm);
