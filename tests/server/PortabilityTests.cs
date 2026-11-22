using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Seryn.Server.Modules.Auth.Contracts;
using Seryn.Server.Modules.Classifications.Contracts;
using Seryn.Server.Modules.Import.Contracts;
using Seryn.Server.Modules.Objectives.Contracts;
using Seryn.Server.Modules.Records.Contracts;

namespace Seryn.Tests;

public sealed class PortabilityTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-portability-" + Guid.NewGuid());
    private const string Password = "Uma frase de teste segura 123!";

    [Fact]
    public async Task Xlsx_round_trip_restores_all_user_data_sheets_without_host_data()
    {
        var destinationDirectory = Path.Combine(Path.GetTempPath(), "seryn-workbook-empty-" + Guid.NewGuid());
        var recordsOnlyDirectory = Path.Combine(Path.GetTempPath(), "seryn-workbook-records-" + Guid.NewGuid());
        await using var source = new TestApplication(directory);
        using var client = source.CreateClient();
        await Login(client);
        var type = await Classification(client, "/api/v1/types", "Compra");
        var payment = await Classification(client, "/api/v1/payment-methods", "Pix");
        var root = await Category(client, "Casa");
        var child = await Category(client, "Escritório", root.Id);
        var recordResponse = await Send(client, HttpMethod.Post, "/api/v1/records", new { nome = "Mesa", movimentacao = "saida", tipoId = type.Id, categoriaId = child.Id, pagamentoId = payment.Id, valorCentavos = 250050, data = "2027-03", recorrencia = "mensal", ocorrencia = "definida", parcelas = 3 });
        Assert.Equal(HttpStatusCode.Created, recordResponse.StatusCode);
        var objectiveResponse = await Send(client, HttpMethod.Post, "/api/v1/objectives", new { nome = "Notebook", valorEstimadoCentavos = 700000, interesse = "desejado", prioridade = 2, link = "https://example.com", observacoes = "Comparar opções", dataDesejada = "2027-08" });
        var objective = (await objectiveResponse.Content.ReadFromJsonAsync<ObjectiveItem>())!;

        var bytes = await client.GetByteArrayAsync("/api/v1/portability/export.xlsx");
        byte[] macroEnabledBytes;
        using (var workbook = new XLWorkbook(new MemoryStream(bytes)))
        {
            Assert.Equal(new[] { "Registros", "Objetivos", "Tipos", "Categorias", "Pagamentos" }, workbook.Worksheets.Select(x => x.Name));
            Assert.DoesNotContain(workbook.Worksheets.SelectMany(x => x.Row(1).CellsUsed()).Select(x => x.GetString()), header => header.Equals("id", StringComparison.OrdinalIgnoreCase) || header.EndsWith("_id", StringComparison.OrdinalIgnoreCase));
            workbook.AddWorksheet("Conteúdo externo").Cell("A1").FormulaA1 = "1+1";
            var macroEnabledPath = Path.Combine(directory, "seryn-dados.xlsm");
            workbook.SaveAs(macroEnabledPath);
            macroEnabledBytes = await File.ReadAllBytesAsync(macroEnabledPath);
        }

        await using (var destination = new TestApplication(destinationDirectory))
        {
            using var destinationClient = destination.CreateClient();
            await Login(destinationClient);
            var payload = Convert.ToBase64String(bytes);
            var preview = (await (await Send(destinationClient, HttpMethod.Post, "/api/v1/portability/workbook/preview", new { conteudoBase64 = payload })).Content.ReadFromJsonAsync<WorkbookPreview>())!;
            Assert.Empty(preview.Erros);
            Assert.Equal(5, preview.Abas.Count);
            Assert.Equal(6, preview.Criacoes);
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(destinationClient, HttpMethod.Post, "/api/v1/portability/workbook/commit", new { conteudoBase64 = payload, previewId = preview.PreviewIdMesclagem, confirmarCriacoes = false })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Send(destinationClient, HttpMethod.Post, "/api/v1/portability/workbook/commit", new { conteudoBase64 = payload, previewId = preview.PreviewIdMesclagem, confirmarCriacoes = true })).StatusCode);

            var restoredRecord = (await destinationClient.GetFromJsonAsync<List<RecordItem>>("/api/v1/records"))!.Single();
            Assert.Equal(250050, restoredRecord.ValorCentavos);
            Assert.Equal("Escritório", restoredRecord.CategoriaNome);
            var restoredObjective = (await destinationClient.GetFromJsonAsync<List<ObjectiveItem>>("/api/v1/objectives"))!.Single();
            Assert.Equal(objective.DataInclusao, restoredObjective.DataInclusao);
            Assert.Equal("Comparar opções", restoredObjective.Observacoes);
            var repeated = (await (await Send(destinationClient, HttpMethod.Post, "/api/v1/portability/workbook/preview", new { conteudoBase64 = payload })).Content.ReadFromJsonAsync<WorkbookPreview>())!;
            Assert.Equal(0, repeated.Criacoes);
            Assert.Equal(6, repeated.Existentes);
            Assert.True(repeated.Completa);

            var macroEnabledPreview = (await (await Send(destinationClient, HttpMethod.Post, "/api/v1/portability/workbook/preview", new { conteudoBase64 = Convert.ToBase64String(macroEnabledBytes) })).Content.ReadFromJsonAsync<WorkbookPreview>())!;
            Assert.Empty(macroEnabledPreview.Erros);
            Assert.Equal(6, macroEnabledPreview.Existentes);

            using var formulaWorkbook = new XLWorkbook(new MemoryStream(macroEnabledBytes));
            formulaWorkbook.Worksheet("Registros").Cell(2, 1).FormulaA1 = "\"Mesa\"";
            using var formulaStream = new MemoryStream();
            formulaWorkbook.SaveAs(formulaStream);
            var formulaPreview = (await (await Send(destinationClient, HttpMethod.Post, "/api/v1/portability/workbook/preview", new { conteudoBase64 = Convert.ToBase64String(formulaStream.ToArray()) })).Content.ReadFromJsonAsync<WorkbookPreview>())!;
            Assert.Contains(formulaPreview.Erros, error => error.Contains("fórmulas não são permitidas"));

            var staleType = await Classification(destinationClient, "/api/v1/types", "Antigo");
            var stalePayment = await Classification(destinationClient, "/api/v1/payment-methods", "Dinheiro");
            var staleCategory = await Category(destinationClient, "Descartar");
            Assert.Equal(HttpStatusCode.Created, (await Send(destinationClient, HttpMethod.Post, "/api/v1/records", new { nome = "Registro antigo", movimentacao = "saida", tipoId = staleType.Id, categoriaId = staleCategory.Id, pagamentoId = stalePayment.Id, valorCentavos = 100, data = "2026-01", recorrencia = "unica", ocorrencia = "definida", parcelas = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await Send(destinationClient, HttpMethod.Post, "/api/v1/objectives", new { nome = "Objetivo antigo", valorEstimadoCentavos = 100, interesse = "listado", prioridade = 0 })).StatusCode);
            var replacementPreview = (await (await Send(destinationClient, HttpMethod.Post, "/api/v1/portability/workbook/preview", new { conteudoBase64 = payload })).Content.ReadFromJsonAsync<WorkbookPreview>())!;
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(destinationClient, HttpMethod.Post, "/api/v1/portability/workbook/commit", new { conteudoBase64 = payload, previewId = replacementPreview.PreviewIdSubstituicao, substituirDados = true, confirmarSubstituicao = false })).StatusCode);
            Assert.Equal(2, (await destinationClient.GetFromJsonAsync<List<RecordItem>>("/api/v1/records"))!.Count);

            var replacementResponse = await Send(destinationClient, HttpMethod.Post, "/api/v1/portability/workbook/commit", new { conteudoBase64 = payload, previewId = replacementPreview.PreviewIdSubstituicao, substituirDados = true, confirmarSubstituicao = true });
            Assert.Equal(HttpStatusCode.OK, replacementResponse.StatusCode);
            var replacement = (await replacementResponse.Content.ReadFromJsonAsync<ImportCommitResult>())!;
            Assert.Equal(11, replacement.Excluidos);
            Assert.Equal(6, replacement.Criados);
            Assert.Equal("Mesa", (await destinationClient.GetFromJsonAsync<List<RecordItem>>("/api/v1/records"))!.Single().Nome);
            Assert.Equal("Notebook", (await destinationClient.GetFromJsonAsync<List<ObjectiveItem>>("/api/v1/objectives"))!.Single().Nome);
            Assert.Equal("Compra", (await destinationClient.GetFromJsonAsync<List<ClassificationItem>>("/api/v1/types"))!.Single().Nome);
        }
        using (var workbook = new XLWorkbook(new MemoryStream(bytes)))
        {
            foreach (var sheet in new[] { "Tipos", "Categorias", "Pagamentos", "Objetivos" }) workbook.Worksheet(sheet).Delete();
            using var stream = new MemoryStream(); workbook.SaveAs(stream);
            await using var recordsOnly = new TestApplication(recordsOnlyDirectory);
            using var recordsClient = recordsOnly.CreateClient(); await Login(recordsClient);
            var payload = Convert.ToBase64String(stream.ToArray());
            var recordsPreview = (await (await Send(recordsClient, HttpMethod.Post, "/api/v1/portability/workbook/preview", new { conteudoBase64 = payload })).Content.ReadFromJsonAsync<WorkbookPreview>())!;
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(recordsClient, HttpMethod.Post, "/api/v1/portability/workbook/commit", new { conteudoBase64 = payload, previewId = recordsPreview.PreviewIdMesclagem, substituirDados = true, confirmarSubstituicao = true })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Send(recordsClient, HttpMethod.Post, "/api/v1/portability/workbook/commit", new { conteudoBase64 = payload, previewId = recordsPreview.PreviewIdMesclagem, confirmarCriacoes = true })).StatusCode);
            var restored = (await recordsClient.GetFromJsonAsync<List<RecordItem>>("/api/v1/records"))!.Single();
            Assert.Equal("Compra", restored.TipoNome); Assert.Equal("Escritório", restored.CategoriaNome); Assert.Equal("Pix", restored.PagamentoNome);
        }
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(destinationDirectory)) Directory.Delete(destinationDirectory, true);
        if (Directory.Exists(recordsOnlyDirectory)) Directory.Delete(recordsOnlyDirectory, true);
    }

    private static async Task<ClassificationItem> Classification(HttpClient client, string path, string name) =>
        (await (await Send(client, HttpMethod.Post, path, new { nome = name })).Content.ReadFromJsonAsync<ClassificationItem>())!;
    private static async Task<CategoryItem> Category(HttpClient client, string name, Guid? parentId = null) =>
        (await (await Send(client, HttpMethod.Post, "/api/v1/categories", new { nome = name, categoriaPaiId = parentId })).Content.ReadFromJsonAsync<CategoryItem>())!;
    private static async Task Login(HttpClient client) => Assert.Equal(HttpStatusCode.OK, (await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "daniel", password = Password })).StatusCode);
    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object data)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(data) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        return await client.SendAsync(request);
    }
    public void Dispose() { SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
