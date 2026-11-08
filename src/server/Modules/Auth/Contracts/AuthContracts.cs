namespace Seryn.Server.Modules.Auth.Contracts;

public sealed record LoginRequest(string? Username, string? Password);
public sealed record AuthStatus(bool Configured);
public sealed record CsrfResponse(string Token);
public sealed record SessionResponse(string Username);
