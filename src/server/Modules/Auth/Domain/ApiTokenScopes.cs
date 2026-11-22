namespace Seryn.Server.Modules.Auth.Domain;

public static class ApiTokenScopes
{
    public const string Read = "data:read";
    public const string Write = "data:write";
    public const string Delete = "data:delete";
    public static readonly string[] All = [Read, Write, Delete];
}
