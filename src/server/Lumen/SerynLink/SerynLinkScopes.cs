namespace Seryn.Server.Lumen.SerynLink;

public static class SerynLinkScopes
{
    public const string Read = "data:read";
    public const string Write = "data:write";
    public const string Delete = "data:delete";
    public static readonly string[] All = [Read, Write, Delete];
}
