namespace Seryn.Server.Modules.Auth.Contracts;

public sealed record LoginRequest(string? Username, string? Password);
public sealed record AuthStatus(bool Configured);
public sealed record CsrfResponse(string Token);
public sealed record SessionResponse(string Username);
public sealed record LoginSessionItem(string Id, string CriadaEm, string UltimoUsoEm, string ExpiraEm, bool Atual);
public sealed record ApiTokenRequest(string? Nome, string[]? Escopos, string? ExpiraEm);
public sealed record ApiTokenItem(Guid Id, string Nome, string Prefixo, string[] Escopos, string CriadoEm, string? ExpiraEm, string? UltimoUsoEm);
public sealed record ApiTokenCreated(Guid Id, string Nome, string Token, string Prefixo, string[] Escopos, string CriadoEm, string? ExpiraEm);
