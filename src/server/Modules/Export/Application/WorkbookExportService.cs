using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Objectives.Contracts;
using Seryn.Server.Modules.Records.Domain;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Export.Application;

internal sealed class WorkbookExportService(SerynDbContext db)
{
    public async Task<byte[]> Export()
    {
        using var workbook = new XLWorkbook();
        await ExportRecords(workbook);
        await ExportObjectives(workbook);
        await ExportTypes(workbook);
        await ExportCategories(workbook);
        await ExportPayments(workbook);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task ExportTypes(XLWorkbook workbook) { var sheet = workbook.AddWorksheet("Tipos"); WriteHeader(sheet, ["nome"]); var row = 2; foreach (var x in await db.FinancialTypes.AsNoTracking().OrderBy(x => x.Name).ToListAsync()) sheet.Cell(row++, 1).Value = Safe(x.Name); Finish(sheet); }
    private async Task ExportPayments(XLWorkbook workbook) { var sheet = workbook.AddWorksheet("Pagamentos"); WriteHeader(sheet, ["nome"]); var row = 2; foreach (var x in await db.PaymentMethods.AsNoTracking().OrderBy(x => x.Name).ToListAsync()) sheet.Cell(row++, 1).Value = Safe(x.Name); Finish(sheet); }
    private async Task ExportCategories(XLWorkbook workbook) { var sheet = workbook.AddWorksheet("Categorias"); WriteHeader(sheet, ["nome", "categoria_pai"]); var row = 2; foreach (var x in await db.FinancialCategories.AsNoTracking().Include(x => x.Parent).OrderBy(x => x.ParentId != null).ThenBy(x => x.Name).ToListAsync()) { sheet.Cell(row, 1).Value = Safe(x.Name); sheet.Cell(row++, 2).Value = Safe(x.Parent?.Name ?? ""); } Finish(sheet); }
    private async Task ExportRecords(XLWorkbook workbook) { var sheet = workbook.AddWorksheet("Registros"); WriteHeader(sheet, ["nome", "movimentacao", "valor", "data", "recorrencia", "parcelas", "tipo", "categoria_pai", "categoria", "pagamento"]); var row = 2; foreach (var x in await db.FinancialRecords.AsNoTracking().Include(x => x.Type).Include(x => x.Category).ThenInclude(x => x!.Parent).Include(x => x.PaymentMethod).OrderBy(x => x.CreatedAtUnix).ToListAsync()) { sheet.Cell(row, 1).Value = Safe(x.Name); sheet.Cell(row, 2).Value = Direction(x.Direction); sheet.Cell(row, 3).Value = x.AmountCents / 100m; sheet.Cell(row, 3).Style.NumberFormat.Format = "R$ #,##0.00"; sheet.Cell(row, 4).Value = $"{x.StartYear:0000}-{x.StartMonth:00}"; sheet.Cell(row, 5).Value = Recurrence(x.Recurrence); if (x.OccurrenceCount is { } count) sheet.Cell(row, 6).Value = count; sheet.Cell(row, 7).Value = Safe(x.Type.Name); sheet.Cell(row, 8).Value = Safe(x.Category?.Parent?.Name ?? ""); sheet.Cell(row, 9).Value = Safe(x.Category?.Name ?? ""); sheet.Cell(row++, 10).Value = Safe(x.PaymentMethod.Name); } Finish(sheet); }
    private async Task ExportObjectives(XLWorkbook workbook) { var sheet = workbook.AddWorksheet("Objetivos"); WriteHeader(sheet, ["nome", "valor_estimado", "interesse", "prioridade", "link", "observacoes", "data_desejada", "data_inclusao"]); var row = 2; foreach (var x in await db.Objectives.AsNoTracking().OrderBy(x => x.CreatedAtUnix).ToListAsync()) { sheet.Cell(row, 1).Value = Safe(x.Name); sheet.Cell(row, 2).Value = x.EstimatedValueCents / 100m; sheet.Cell(row, 2).Style.NumberFormat.Format = "R$ #,##0.00"; sheet.Cell(row, 3).Value = Interest(x.Interest); sheet.Cell(row, 4).Value = x.Priority; sheet.Cell(row, 5).Value = Safe(x.Link ?? ""); sheet.Cell(row, 6).Value = Safe(x.Notes ?? ""); sheet.Cell(row, 7).Value = x.DesiredYear is { } year && x.DesiredMonth is { } month ? $"{year:0000}-{month:00}" : ""; sheet.Cell(row++, 8).Value = DateTimeOffset.FromUnixTimeSeconds(x.CreatedAtUnix).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); } Finish(sheet); }

    private static string Safe(string value) { var protect = value.Length > 0 && "=+-@\t\r".Contains(value[0]) || value.Length > 1 && value[0] == '\'' && "=+-@\t\r".Contains(value[1]); return protect ? "'" + value : value; }
    private static void WriteHeader(IXLWorksheet sheet, string[] headers) { for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column]; sheet.Row(1).Style.Font.Bold = true; sheet.SheetView.FreezeRows(1); }
    private static void Finish(IXLWorksheet sheet) { sheet.ColumnsUsed().AdjustToContents(1, 60); sheet.RangeUsed()?.SetAutoFilter(); }
    private static string Direction(RecordDirection value) => value == RecordDirection.Income ? "entrada" : "saida";
    private static string Recurrence(RecordRecurrence value) => value switch { RecordRecurrence.Once => "unica", RecordRecurrence.Monthly => "mensal", RecordRecurrence.Quarterly => "trimestral", RecordRecurrence.Semiannual => "semestral", _ => "anual" };
    private static string Interest(ObjectiveInterest value) => value switch { ObjectiveInterest.Listed => "listado", ObjectiveInterest.Considered => "considerado", _ => "desejado" };
}
