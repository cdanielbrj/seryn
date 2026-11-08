using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Seryn.Server.Modules.Auth.Contracts;
using Seryn.Server.Modules.Classifications.Contracts;
using Seryn.Server.Modules.Projections.Contracts;

namespace Seryn.Tests;

public sealed class ProjectionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-projections-" + Guid.NewGuid());
    private const string Password = "Uma frase de teste segura 123!";

    [Fact]
    public async Task Projections_use_real_occurrences_for_periods_chart_categories_and_rankings()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        await Login(client);
        var type = await Create(client, "/api/v1/types", "Assinatura");
        var payment = await Create(client, "/api/v1/payment-methods", "Pix");
        var software = await CreateCategory(client, "Software");
        var server = await CreateCategory(client, "Servidor", software.Id);
        await Record(client, "Salário", "entrada", type.Id, payment.Id, 800000, "2027-01", "mensal", "indefinida", null);
        await Record(client, "GPU", "saida", type.Id, payment.Id, 75000, "2027-01", "mensal", "definida", 10, server.Id);
        await Record(client, "Venda", "entrada", type.Id, payment.Id, 200000, "2027-02", "unica", "definida", 1);

        var month = await client.GetFromJsonAsync<DashboardProjection>("/api/v1/projections?period=month&reference=2027-02");
        Assert.NotNull(month);
        Assert.Equal(1_000_000, month.EntradasCentavos);
        Assert.Equal(75_000, month.SaidasCentavos);
        Assert.Equal(925_000, month.ResultadoCentavos);
        Assert.Equal(7, month.Grafico.Count);
        Assert.Equal("2026-11", month.GraficoInicio);
        Assert.Equal("2027-05", month.GraficoFim);
        Assert.Single(month.Grafico, x => x.Selecionado && x.Mes == "2027-02");

        var semester = await client.GetFromJsonAsync<DashboardProjection>("/api/v1/projections?period=semester&reference=2027-02");
        Assert.NotNull(semester);
        Assert.Equal(5_000_000, semester.EntradasCentavos);
        Assert.Equal(450_000, semester.SaidasCentavos);
        Assert.Equal(4_550_000, semester.ResultadoCentavos);
        Assert.Equal(semester.ResultadoCentavos, semester.Grafico.Last().ResultadoAcumuladoCentavos);
        var category = Assert.Single(semester.Categorias);
        Assert.Equal("Software", category.Nome);
        Assert.Equal(450_000, category.ValorCentavos);
        Assert.Equal(100, category.Percentual);
        Assert.Equal("Salário", semester.MaioresEntradas[0].Nome);
        Assert.Equal(4_800_000, semester.MaioresEntradas[0].ValorCentavos);
        Assert.Equal("GPU", Assert.Single(semester.MaioresSaidas).Nome);
        Assert.Equal(6, semester.MaioresSaidas.Single().Ocorrencias);

        var analysis = await client.GetFromJsonAsync<AnalysisProjection>("/api/v1/analyses?period=semester&reference=2027-02&direction=expense&primary=category&secondary=payment");
        Assert.NotNull(analysis);
        Assert.Equal(450_000, analysis.SaidasCentavos);
        Assert.Equal(-450_000, analysis.ResultadoCentavos);
        Assert.Equal(1, analysis.Registros);
        Assert.Equal(6, analysis.Ocorrencias);
        var segment = Assert.Single(analysis.Segmentos);
        Assert.Equal("Software", segment.Nome);
        Assert.Equal(100, segment.Percentual);
        Assert.Equal("Pix", Assert.Single(segment.Cruzamentos).Nome);
        var analysisRecord = Assert.Single(segment.Detalhes);
        Assert.Equal("GPU", analysisRecord.Nome);
        Assert.Equal(10, analysisRecord.ParcelasTotais);
        Assert.Equal(6, analysisRecord.ParcelasPrevistasAteFim);

        var invalidAnalysis = await client.GetAsync("/api/v1/analyses?period=month&reference=2027-02&direction=all&primary=type&secondary=type");
        Assert.Equal(HttpStatusCode.BadRequest, invalidAnalysis.StatusCode);
        Assert.Contains("secondary", (await invalidAnalysis.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);

        var beforeStart = await client.GetFromJsonAsync<DashboardProjection>("/api/v1/projections?period=month&reference=2026-12");
        Assert.NotNull(beforeStart);
        Assert.Equal(0, beforeStart.EntradasCentavos);
        Assert.Equal(0, beforeStart.SaidasCentavos);

        var invalid = await client.GetAsync("/api/v1/projections?period=week&reference=2027-02");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("period", (await invalid.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    private static async Task Record(HttpClient client, string nome, string movimentacao, Guid tipoId, Guid pagamentoId, long valorCentavos, string data, string recorrencia, string ocorrencia, int? parcelas, Guid? categoriaId = null)
    {
        var response = await Send(client, HttpMethod.Post, "/api/v1/records", new { nome, movimentacao, tipoId, categoriaId, pagamentoId, valorCentavos, data, recorrencia, ocorrencia, parcelas });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<ClassificationItem> Create(HttpClient client, string path, string nome)
    {
        var response = await Send(client, HttpMethod.Post, path, new { nome });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClassificationItem>())!;
    }

    private static async Task<CategoryItem> CreateCategory(HttpClient client, string nome, Guid? categoriaPaiId = null)
    {
        var response = await Send(client, HttpMethod.Post, "/api/v1/categories", new { nome, categoriaPaiId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryItem>())!;
    }

    private static async Task Login(HttpClient client)
    {
        var response = await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "daniel", password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object? data = null)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        if (data is not null) request.Content = JsonContent.Create(data);
        return await client.SendAsync(request);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
