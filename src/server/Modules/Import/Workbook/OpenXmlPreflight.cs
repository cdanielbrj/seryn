using System.IO.Compression;
using System.Xml;

namespace Seryn.Server.Modules.Import.Workbook;

internal static class OpenXmlPreflight
{
    private const int MaxEntries = 100;
    private const long MaxExpandedBytes = 100L * 1024 * 1024;
    private const long MaxEntryBytes = 50L * 1024 * 1024;
    private const int MaxWorksheets = 10;
    private const int MaxRows = 50_000;
    private const int MaxCells = 500_000;
    private const double MaxCompressionRatio = 200;

    public static string? Validate(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            if (archive.Entries.Count is 0 or > MaxEntries)
                return $"A planilha deve conter no máximo {MaxEntries} partes internas.";
            long expanded = 0;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var worksheets = 0;
            var rows = 0;
            var cells = 0;
            long dimensionCells = 0;
            foreach (var entry in archive.Entries)
            {
                var name = entry.FullName.Replace('\\', '/');
                if (name.StartsWith('/') || name.Split('/').Any(segment => segment is ".." or ".") || !names.Add(name))
                    return "A planilha contém caminhos internos inválidos ou duplicados.";
                if (entry.Length > MaxEntryBytes) return "A planilha contém uma parte interna maior que 50 MB.";
                expanded += entry.Length;
                if (expanded > MaxExpandedBytes) return "A planilha excede 100 MB após descompactação.";
                if (entry.Length > 1024 * 1024 && entry.Length / (double)Math.Max(1, entry.CompressedLength) > MaxCompressionRatio)
                    return "A planilha possui uma taxa de compressão insegura.";
                if (name.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    worksheets++;
                    if (worksheets > MaxWorksheets) return $"A planilha deve conter no máximo {MaxWorksheets} abas.";
                    CountWorksheet(entry, ref rows, ref cells, ref dimensionCells);
                    if (rows > MaxRows) return $"A planilha deve conter no máximo {MaxRows:N0} linhas usadas.";
                    if (cells > MaxCells) return $"A planilha deve conter no máximo {MaxCells:N0} células usadas.";
                    if (dimensionCells > MaxCells) return $"As dimensões declaradas da planilha excedem {MaxCells:N0} células.";
                }
                if (name.EndsWith(".rels", StringComparison.OrdinalIgnoreCase) && HasExternalRelationship(entry))
                    return "A planilha contém relações externas, que não são permitidas.";
            }
            return null;
        }
        catch (Exception error) when (error is InvalidDataException or XmlException or IOException)
        {
            return "O pacote OpenXML da planilha é inválido.";
        }
    }

    private static void CountWorksheet(ZipArchiveEntry entry, ref int rows, ref int cells, ref long dimensionCells)
    {
        using var reader = CreateReader(entry);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;
            if (reader.LocalName == "dimension" && reader.GetAttribute("ref") is { } reference)
                dimensionCells += DimensionSize(reference);
            else if (reader.LocalName == "row") rows++;
            else if (reader.LocalName == "c") cells++;
        }
    }

    private static long DimensionSize(string reference)
    {
        var bounds = reference.Replace("$", "", StringComparison.Ordinal).Split(':');
        if (bounds.Length is < 1 or > 2) throw new XmlException("Invalid worksheet dimension.");
        var start = Cell(bounds[0]);
        var end = Cell(bounds[^1]);
        if (end.Row < start.Row || end.Column < start.Column) throw new XmlException("Invalid worksheet dimension.");
        return (long)(end.Row - start.Row + 1) * (end.Column - start.Column + 1);

        static (int Row, int Column) Cell(string value)
        {
            var split = 0;
            while (split < value.Length && char.IsLetter(value[split])) split++;
            if (split == 0 || split == value.Length || !int.TryParse(value[split..], out var row) || row < 1)
                throw new XmlException("Invalid worksheet cell reference.");
            var column = 0;
            foreach (var letter in value[..split].ToUpperInvariant()) column = checked(column * 26 + letter - 'A' + 1);
            return (row, column);
        }
    }

    private static bool HasExternalRelationship(ZipArchiveEntry entry)
    {
        using var reader = CreateReader(entry);
        while (reader.Read())
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Relationship" &&
                string.Equals(reader.GetAttribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static XmlReader CreateReader(ZipArchiveEntry entry) => XmlReader.Create(entry.Open(), new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = MaxEntryBytes,
        IgnoreComments = true,
        IgnoreWhitespace = true
    });
}
