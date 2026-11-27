using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Seryn.Server.Modules.Auth.Contracts;
using Seryn.Server.Lumen.SerynLink.Contracts;
using Seryn.Server.Modules.Classifications.Contracts;
using Seryn.Server.Modules.Import.Contracts;
using Seryn.Server.Modules.Objectives.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Tests;

public sealed class PortabilitySecurityTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-portability-security-" + Guid.NewGuid());
    private const string Password = "Uma frase de teste segura 123!";

    [Fact]
    public async Task Preview_is_bound_to_session_file_mode_current_state_and_single_use()
    {
        await using var app = new TestApplication(directory);
        using var first = app.CreateClient();
        using var second = app.CreateClient();
        await Login(first); await Login(second);
        var payload = Convert.ToBase64String(Workbook());
        var preview = await Preview(first, payload);

        Assert.Equal(HttpStatusCode.BadRequest, (await Commit(second, payload, preview.PreviewIdMesclagem!.Value, false)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Commit(first, Convert.ToBase64String(Workbook("Outro")), preview.PreviewIdMesclagem.Value, false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Commit(first, payload, preview.PreviewIdMesclagem.Value, false)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Commit(first, payload, preview.PreviewIdMesclagem.Value, false)).StatusCode);

        var stalePreview = await Preview(first, payload);
        Assert.Equal(HttpStatusCode.Created, (await Send(first, HttpMethod.Post, "/api/v1/types", new { nome = "Mudança concorrente" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Commit(first, payload, stalePreview.PreviewIdSubstituicao!.Value, true)).StatusCode);
    }

    [Fact]
    public async Task Concurrent_import_commits_are_serialized_per_portable_dataset()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        await Login(client);
        var payload = Convert.ToBase64String(Workbook("Concorrente"));
        var previews = new List<WorkbookPreview>();
        for (var index = 0; index < 12; index++) previews.Add(await Preview(client, payload));

        var responses = await Task.WhenAll(previews.Select(preview =>
            Commit(client, payload, preview.PreviewIdMesclagem!.Value, false)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(11, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
        Assert.Single((await client.GetFromJsonAsync<List<ObjectiveItem>>("/api/v1/objectives"))!, objective => objective.Nome == "Concorrente");
    }

    [Fact]
    public async Task Replacement_rolls_back_every_user_table_when_a_late_database_failure_occurs()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        await Login(client);
        Assert.Equal(HttpStatusCode.Created, (await Send(client, HttpMethod.Post, "/api/v1/lumen/seryn-links", new { nome = "Preservar", escopos = new[] { "data:read" } })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Send(client, HttpMethod.Post, "/api/v1/types", new { nome = "Original" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Send(client, HttpMethod.Post, "/api/v1/objectives", new { nome = "Objetivo original", valorEstimadoCentavos = 10000, interesse = "listado", prioridade = 0 })).StatusCode);
        var payload = Convert.ToBase64String(Workbook("Objetivo novo"));
        var preview = await Preview(client, payload);

        await using (var scope = app.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<SerynDbContext>().Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER fail_objective_import BEFORE INSERT ON objectives BEGIN SELECT RAISE(ABORT, 'forced rollback'); END;");

        Assert.Equal(HttpStatusCode.BadRequest, (await Commit(client, payload, preview.PreviewIdSubstituicao!.Value, true)).StatusCode);
        Assert.Equal("Original", (await client.GetFromJsonAsync<List<ClassificationItem>>("/api/v1/types"))!.Single().Nome);
        Assert.Equal("Objetivo original", (await client.GetFromJsonAsync<List<ObjectiveItem>>("/api/v1/objectives"))!.Single().Nome);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/session")).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<List<SerynLinkItem>>("/api/v1/lumen/seryn-links"))!);
    }

    [Fact]
    public async Task OpenXml_preflight_rejects_external_relationships_and_excessive_complexity()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        await Login(client);

        var external = AddExternalRelationship(Workbook());
        Assert.Contains("relações externas", (await Preview(client, Convert.ToBase64String(external))).Erros.Single());

        var excessiveEntries = AddEntries(Workbook(), 101);
        Assert.Contains("partes internas", (await Preview(client, Convert.ToBase64String(excessiveEntries))).Erros.Single());

        var highCompression = AddCompressedEntry(Workbook(), 2 * 1024 * 1024);
        Assert.Contains("compressão insegura", (await Preview(client, Convert.ToBase64String(highCompression))).Erros.Single());

        var oversizedDimension = OversizeWorksheetDimension(Workbook());
        Assert.Contains("dimensões declaradas", (await Preview(client, Convert.ToBase64String(oversizedDimension))).Erros.Single());
    }

    [Fact]
    public async Task Macro_parts_hidden_sheets_defined_names_and_custom_xml_are_ignored_not_persisted()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        await Login(client);
        var adversarial = AddIgnoredOpenXmlParts(Workbook());
        var preview = await Preview(client, Convert.ToBase64String(adversarial));
        Assert.Empty(preview.Erros);
        Assert.NotNull(preview.PreviewIdMesclagem);
        Assert.Equal(HttpStatusCode.OK, (await Commit(client, Convert.ToBase64String(adversarial), preview.PreviewIdMesclagem!.Value, false)).StatusCode);
        var exported = await client.GetByteArrayAsync("/api/v1/portability/export.xlsx");
        using var archive = new ZipArchive(new MemoryStream(exported), ZipArchiveMode.Read);
        Assert.DoesNotContain(archive.Entries, entry => entry.FullName.Contains("vbaProject", StringComparison.OrdinalIgnoreCase) || entry.FullName.StartsWith("customXml/", StringComparison.OrdinalIgnoreCase));
    }

    private static byte[] Workbook(string objectiveName = "Objetivo")
    {
        using var workbook = new XLWorkbook();
        AddSheet(workbook, "Registros", ["nome", "movimentacao", "valor", "data", "recorrencia", "parcelas", "tipo", "categoria_pai", "categoria", "pagamento"]);
        var objectives = AddSheet(workbook, "Objetivos", ["nome", "valor_estimado", "interesse", "prioridade", "link", "observacoes", "data_desejada", "data_inclusao"]);
        objectives.Cell(2, 1).Value = objectiveName; objectives.Cell(2, 2).Value = 100; objectives.Cell(2, 3).Value = "listado";
        objectives.Cell(2, 4).Value = 0; objectives.Cell(2, 8).Value = "2026-11-21";
        AddSheet(workbook, "Tipos", ["nome"]);
        AddSheet(workbook, "Categorias", ["nome", "categoria_pai"]);
        AddSheet(workbook, "Pagamentos", ["nome"]);
        using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
    }

    private static IXLWorksheet AddSheet(XLWorkbook workbook, string name, string[] headers)
    {
        var sheet = workbook.AddWorksheet(name);
        for (var index = 0; index < headers.Length; index++) sheet.Cell(1, index + 1).Value = headers[index];
        return sheet;
    }

    private static byte[] AddExternalRelationship(byte[] source) => Rewrite(source, archive =>
    {
        var entry = archive.GetEntry("xl/_rels/workbook.xml.rels")!;
        var xml = Read(entry).Replace("</Relationships>", "<Relationship Id=\"external-test\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/externalLink\" Target=\"https://example.com/data.xlsx\" TargetMode=\"External\"/></Relationships>", StringComparison.Ordinal);
        Replace(archive, entry.FullName, xml);
    });

    private static byte[] AddEntries(byte[] source, int count) => Rewrite(source, archive =>
    {
        for (var index = 0; index < count; index++) archive.CreateEntry($"customXml/item-{index}.xml");
    });

    private static byte[] AddCompressedEntry(byte[] source, int bytes) => Rewrite(source, archive =>
    {
        var entry = archive.CreateEntry("customXml/compressed.bin", CompressionLevel.SmallestSize);
        using var output = entry.Open();
        var block = new byte[8192];
        for (var written = 0; written < bytes; written += block.Length) output.Write(block, 0, Math.Min(block.Length, bytes - written));
    });

    private static byte[] OversizeWorksheetDimension(byte[] source) => Rewrite(source, archive =>
    {
        var entry = archive.GetEntry("xl/worksheets/sheet1.xml")!;
        var xml = Regex.Replace(Read(entry), "<(?:\\w+:)?dimension\\s+[^>]*ref=\"[^\"]+\"[^>]*/>", "<dimension ref=\"A1:XFD1048576\"/>", RegexOptions.CultureInvariant);
        Replace(archive, entry.FullName, xml);
    });

    private static byte[] AddIgnoredOpenXmlParts(byte[] source)
    {
        using var workbook = new XLWorkbook(new MemoryStream(source));
        var hidden = workbook.AddWorksheet("Conteúdo oculto");
        hidden.Cell("A1").FormulaA1 = "1+1";
        hidden.Hide();
        workbook.DefinedNames.Add("NomeIgnorado", hidden.Range("A1"));
        using var prepared = new MemoryStream(); workbook.SaveAs(prepared);
        return Rewrite(prepared.ToArray(), archive =>
        {
            using (var macro = archive.CreateEntry("xl/vbaProject.bin").Open()) macro.Write([0xD0, 0xCF, 0x11, 0xE0]);
            using (var custom = archive.CreateEntry("customXml/item1.xml").Open()) custom.Write(Encoding.UTF8.GetBytes("<custom>ignored</custom>"));
            var contentTypes = Read(archive.GetEntry("[Content_Types].xml")!)
                .Replace("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", "application/vnd.ms-excel.sheet.macroEnabled.main+xml", StringComparison.Ordinal)
                .Replace("</Types>", "<Override PartName=\"/xl/vbaProject.bin\" ContentType=\"application/vnd.ms-office.vbaProject\"/></Types>", StringComparison.Ordinal);
            Replace(archive, "[Content_Types].xml", contentTypes);
            var relationships = Read(archive.GetEntry("xl/_rels/workbook.xml.rels")!)
                .Replace("</Relationships>", "<Relationship Id=\"vba-test\" Type=\"http://schemas.microsoft.com/office/2006/relationships/vbaProject\" Target=\"vbaProject.bin\"/></Relationships>", StringComparison.Ordinal);
            Replace(archive, "xl/_rels/workbook.xml.rels", relationships);
        });
    }

    private static byte[] Rewrite(byte[] source, Action<ZipArchive> update)
    {
        var stream = new MemoryStream(); stream.Write(source); stream.Position = 0;
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true)) update(archive);
        return stream.ToArray();
    }

    private static string Read(ZipArchiveEntry entry) { using var reader = new StreamReader(entry.Open()); return reader.ReadToEnd(); }
    private static void Replace(ZipArchive archive, string name, string content)
    {
        archive.GetEntry(name)!.Delete();
        using var writer = new StreamWriter(archive.CreateEntry(name).Open()); writer.Write(content);
    }

    private static async Task<WorkbookPreview> Preview(HttpClient client, string payload) =>
        (await (await Send(client, HttpMethod.Post, "/api/v1/portability/workbook/preview", new { conteudoBase64 = payload })).Content.ReadFromJsonAsync<WorkbookPreview>())!;
    private static Task<HttpResponseMessage> Commit(HttpClient client, string payload, Guid previewId, bool replace) =>
        Send(client, HttpMethod.Post, "/api/v1/portability/workbook/commit", new { conteudoBase64 = payload, previewId, confirmarCriacoes = true, substituirDados = replace, confirmarSubstituicao = replace });
    private static async Task Login(HttpClient client) => Assert.Equal(HttpStatusCode.OK, (await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "daniel", password = Password })).StatusCode);
    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object data)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(data) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        return await client.SendAsync(request);
    }

    public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
