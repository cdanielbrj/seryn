using System.Text;

namespace Seryn.Server.Modules.Classifications.Application;

internal static class ClassificationName
{
    public static (string Name, string Normalized)? Prepare(string? input)
    {
        var name = input?.Trim().Normalize(NormalizationForm.FormC) ?? "";
        return name.Length is < 1 or > 80 ? null : (name, name.ToUpperInvariant());
    }
}
