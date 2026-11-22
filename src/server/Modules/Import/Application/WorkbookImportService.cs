using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Import.Contracts;
using Seryn.Server.Modules.Import.Workbook;
using Seryn.Server.Modules.Objectives.Application;
using Seryn.Server.Modules.Objectives.Contracts;
using Seryn.Server.Modules.Records.Application;
using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Modules.Records.Domain;
using Seryn.Server.Persistence;
using Seryn.Server.Infrastructure;

namespace Seryn.Server.Modules.Import.Application;

internal sealed class WorkbookImportService(SerynDbContext db, RecordCommandProcessor recordProcessor, TimeProvider clock, AsyncKeyedLock locks)
{
    private const int MaxBytes = 10 * 1024 * 1024;
    private static readonly string[] RequiredSheets = ["Registros", "Objetivos", "Tipos", "Categorias", "Pagamentos"];
    private sealed record CategoryRow(string Name, string ParentName);
    private sealed record RecordRow(string Name, string Direction, long Amount, string Date, string Recurrence, int? Count, string Type, string ParentCategory, string Category, string Payment);
    private sealed record ObjectiveRow(string Name, long Value, string Interest, int Priority, string? Link, string? Notes, string? DesiredDate, DateOnly CreatedDate);
    private sealed record Model(List<string> Types, List<string> Payments, List<CategoryRow> Categories, List<RecordRow> Records, List<ObjectiveRow> Objectives, List<string> Errors, HashSet<string> PresentSheets, string WorkbookHash);
    private sealed record PreviewCalculation(WorkbookPreview Preview, string CurrentDataHash);

    public async Task<(WorkbookPreview? Value, Dictionary<string, string[]>? Errors)> Preview(string? base64, string sessionId)
    {
        var model = Parse(base64);
        if (model.Errors.Count > 0) return (new(null, null, "", [], 0, 0, 0, false, model.Errors), null);

        var calculation = await CalculatePreview(model);
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(30).ToUnixTimeSeconds();
        await db.WorkbookImportPreviews.Where(x => x.ExpiresAtUnix <= now.ToUnixTimeSeconds() || x.ConsumedAtUnix != null).ExecuteDeleteAsync();
        var merge = NewPreview(false);
        db.WorkbookImportPreviews.Add(merge);
        WorkbookImportPreview? replace = null;
        if (calculation.Preview.Completa)
        {
            replace = NewPreview(true);
            db.WorkbookImportPreviews.Add(replace);
        }
        await db.SaveChangesAsync();
        return (calculation.Preview with
        {
            PreviewIdMesclagem = merge.Id,
            PreviewIdSubstituicao = replace?.Id,
            ExpiraEm = DateTimeOffset.FromUnixTimeSeconds(expires).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
        }, null);

        WorkbookImportPreview NewPreview(bool replaceData) => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            WorkbookHash = model.WorkbookHash,
            CurrentDataHash = calculation.CurrentDataHash,
            ReplaceData = replaceData,
            ExpiresAtUnix = expires
        };
    }

    private async Task<PreviewCalculation> CalculatePreview(Model model)
    {

        var typeKeys = (await db.FinancialTypes.AsNoTracking().Select(x => x.NormalizedName).ToListAsync()).ToHashSet();
        var paymentKeys = (await db.PaymentMethods.AsNoTracking().Select(x => x.NormalizedName).ToListAsync()).ToHashSet();
        var categories = await db.FinancialCategories.AsNoTracking().Include(x => x.Parent).ToListAsync();
        var categoryKeys = categories.Select(x => CategoryKey(x.Name, x.Parent?.Name ?? "")).ToHashSet();
        var records = await db.FinancialRecords.AsNoTracking().Include(x => x.Type).Include(x => x.PaymentMethod).Include(x => x.Category).ThenInclude(x => x!.Parent).ToListAsync();
        var recordKeys = records.Select(RecordKey).ToHashSet();
        var objectives = await db.Objectives.AsNoTracking().ToListAsync();
        var objectiveKeys = objectives.Select(ObjectiveKey).ToHashSet();

        var sheets = new List<WorkbookSheetPreview>();
        Add(sheets, model, "Tipos", model.Types.Select(Normalize), typeKeys);
        Add(sheets, model, "Categorias", model.Categories.Select(x => CategoryKey(x.Name, x.ParentName)), categoryKeys);
        Add(sheets, model, "Pagamentos", model.Payments.Select(Normalize), paymentKeys);
        Add(sheets, model, "Registros", model.Records.Select(RecordKey), recordKeys);
        Add(sheets, model, "Objetivos", model.Objectives.Select(ObjectiveKey), objectiveKeys);
        var currentItems = typeKeys.Count + paymentKeys.Count + categoryKeys.Count + recordKeys.Count + objectiveKeys.Count;
        var preview = new WorkbookPreview(null, null, "", sheets, sheets.Sum(x => x.Criacoes), sheets.Sum(x => x.Existentes), currentItems, RequiredSheets.All(model.PresentSheets.Contains), []);
        var stateHash = HashState(typeKeys.Select(x => $"T:{x}"), paymentKeys.Select(x => $"P:{x}"),
            categoryKeys.Select(x => $"C:{x}"), records.Select(x => $"R:{RecordKey(x)}"), objectives.Select(x => $"O:{ObjectiveKey(x)}"));
        return new(preview, stateHash);
    }

    public async Task<(ImportCommitResult? Value, Dictionary<string, string[]>? Errors)> Commit(WorkbookCommitRequest request, string sessionId)
    {
        using var importLock = await locks.Acquire("portable-user-data");
        var model = Parse(request.ConteudoBase64);
        if (model.Errors.Count > 0) return (null, new() { ["arquivo"] = model.Errors.ToArray() });
        var calculation = await CalculatePreview(model);
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var authorization = await db.WorkbookImportPreviews.SingleOrDefaultAsync(x =>
            x.Id == request.PreviewId && x.SessionId == sessionId && x.WorkbookHash == model.WorkbookHash &&
            x.CurrentDataHash == calculation.CurrentDataHash && x.ReplaceData == request.SubstituirDados &&
            x.ConsumedAtUnix == null && x.ExpiresAtUnix > now);
        if (authorization is null)
            return (null, new() { ["previewId"] = ["A prévia expirou, já foi usada ou não corresponde ao arquivo e aos dados atuais. Gere uma nova prévia."] });
        if (request.SubstituirDados && !calculation.Preview.Completa) return (null, new() { ["arquivo"] = ["A substituição exige as cinco abas da planilha completa da Seryn."] });
        if (request.SubstituirDados && !request.ConfirmarSubstituicao) return (null, new() { ["confirmarSubstituicao"] = ["Confirme a exclusão dos dados atuais antes da substituição."] });
        if (!request.SubstituirDados && calculation.Preview.Criacoes > 0 && !request.ConfirmarCriacoes) return (null, new() { ["confirmarCriacoes"] = ["Confirme a criação dos novos dados."] });

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            authorization.ConsumedAtUnix = now;
            var deleted = request.SubstituirDados ? await DeleteUserData() : 0;
            var types = await ApplyClassifications<FinancialType>(model.Types);
            var payments = await ApplyClassifications<PaymentMethod>(model.Payments);
            var categories = await ApplyCategories(model.Categories);
            await db.SaveChangesAsync();
            var (recordCreates, recordExisting) = await ApplyRecords(model.Records, types, categories, payments);
            var (objectiveCreates, objectiveExisting) = await ApplyObjectives(model.Objectives);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            if (request.SubstituirDados) return (new(await CountUserData(), 0, deleted), null);
            var auxiliaryCreates = calculation.Preview.Abas.Where(x => x.Aba is "Tipos" or "Categorias" or "Pagamentos").Sum(x => x.Criacoes);
            var auxiliaryExisting = calculation.Preview.Abas.Where(x => x.Aba is "Tipos" or "Categorias" or "Pagamentos").Sum(x => x.Existentes);
            return (new(auxiliaryCreates + recordCreates + objectiveCreates, auxiliaryExisting + recordExisting + objectiveExisting, 0), null);
        }
        catch (InvalidDataException error)
        {
            await transaction.RollbackAsync();
            return (null, new() { ["arquivo"] = [error.Message] });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return (null, new() { ["arquivo"] = ["A planilha contém classificações conflitantes."] });
        }
    }

    private async Task<int> DeleteUserData()
    {
        var deleted = await db.FinancialRecords.ExecuteDeleteAsync();
        deleted += await db.Objectives.ExecuteDeleteAsync();
        deleted += await db.FinancialCategories.Where(x => x.ParentId != null).ExecuteDeleteAsync();
        deleted += await db.FinancialCategories.ExecuteDeleteAsync();
        deleted += await db.FinancialTypes.ExecuteDeleteAsync();
        deleted += await db.PaymentMethods.ExecuteDeleteAsync();
        return deleted;
    }

    private async Task<int> CountUserData() =>
        await db.FinancialRecords.CountAsync() + await db.Objectives.CountAsync() +
        await db.FinancialCategories.CountAsync() + await db.FinancialTypes.CountAsync() + await db.PaymentMethods.CountAsync();

    private async Task<Dictionary<string, T>> ApplyClassifications<T>(IEnumerable<string> names) where T : class, IClassification, new()
    {
        var values = await db.Set<T>().ToListAsync();
        var result = values.ToDictionary(x => x.NormalizedName);
        foreach (var name in names)
        {
            var key = Normalize(name);
            if (result.ContainsKey(key)) continue;
            var entity = new T { Id = Guid.NewGuid(), Name = name, NormalizedName = key };
            db.Add(entity); result[key] = entity;
        }
        return result;
    }

    private async Task<Dictionary<string, FinancialCategory>> ApplyCategories(List<CategoryRow> rows)
    {
        var values = await db.FinancialCategories.Include(x => x.Parent).ToListAsync();
        var result = values.ToDictionary(x => CategoryKey(x.Name, x.Parent?.Name ?? ""));
        foreach (var row in rows.Where(x => x.ParentName.Length == 0)) Resolve(row, null);
        foreach (var row in rows.Where(x => x.ParentName.Length > 0))
        {
            var parent = Resolve(new(row.ParentName, ""), null);
            Resolve(row, parent);
        }
        return result;

        FinancialCategory Resolve(CategoryRow row, FinancialCategory? parent)
        {
            var key = CategoryKey(row.Name, parent?.Name ?? "");
            if (result.TryGetValue(key, out var existing)) return existing;
            var entity = NewCategory(row.Name, parent);
            db.FinancialCategories.Add(entity); result[key] = entity; return entity;
        }
    }

    private async Task<(int Created, int Existing)> ApplyRecords(List<RecordRow> rows, Dictionary<string, FinancialType> types, Dictionary<string, FinancialCategory> categories, Dictionary<string, PaymentMethod> payments)
    {
        var values = await db.FinancialRecords.Include(x => x.Type).Include(x => x.PaymentMethod).Include(x => x.Category).ThenInclude(x => x!.Parent).ToListAsync();
        var existing = values.Select(RecordKey).ToHashSet();
        var created = 0; var skipped = 0;
        foreach (var row in rows)
        {
            if (!existing.Add(RecordKey(row))) { skipped++; continue; }
            var type = GetClassification(types, row.Type, () => new FinancialType { Id = Guid.NewGuid(), Name = row.Type, NormalizedName = Normalize(row.Type) });
            var payment = GetClassification(payments, row.Payment, () => new PaymentMethod { Id = Guid.NewGuid(), Name = row.Payment, NormalizedName = Normalize(row.Payment) });
            FinancialCategory? category = null;
            if (row.Category.Length > 0)
            {
                FinancialCategory? parent = null;
                if (row.ParentCategory.Length > 0) parent = GetCategory(categories, row.ParentCategory, "", () => NewCategory(row.ParentCategory, null));
                category = GetCategory(categories, row.Category, row.ParentCategory, () => NewCategory(row.Category, parent));
            }
            await db.SaveChangesAsync();
            var command = new RecordCommand(row.Name, row.Direction, type.Id, category?.Id, payment.Id, row.Amount, row.Date, row.Recurrence, row.Count is null ? "indefinida" : "definida", row.Count, "por_ocorrencia");
            var prepared = await recordProcessor.Prepare(command);
            if (prepared.Errors.Count > 0) throw new InvalidDataException($"Registro '{row.Name}': {string.Join(' ', prepared.Errors.Values.SelectMany(x => x))}");
            var now = clock.GetUtcNow().ToUnixTimeSeconds(); var entity = new FinancialRecord { Id = Guid.NewGuid(), CreatedAtUnix = now, UpdatedAtUnix = now };
            RecordCommandProcessor.Apply(entity, prepared.Value!); db.FinancialRecords.Add(entity); created++;
        }
        return (created, skipped);

        T GetClassification<T>(Dictionary<string, T> map, string name, Func<T> create) where T : class
        {
            var key = Normalize(name); if (map.TryGetValue(key, out var item)) return item;
            item = create(); map[key] = item; db.Add(item); return item;
        }
        FinancialCategory GetCategory(Dictionary<string, FinancialCategory> map, string name, string parent, Func<FinancialCategory> create)
        {
            var key = CategoryKey(name, parent); if (map.TryGetValue(key, out var item)) return item;
            item = create(); map[key] = item; db.Add(item); return item;
        }
    }

    private async Task<(int Created, int Existing)> ApplyObjectives(List<ObjectiveRow> rows)
    {
        var values = await db.Objectives.ToListAsync(); var existing = values.Select(ObjectiveKey).ToHashSet();
        var created = 0; var skipped = 0;
        foreach (var row in rows)
        {
            if (!existing.Add(ObjectiveKey(row))) { skipped++; continue; }
            var prepared = ObjectiveService.Prepare(new ObjectiveCommand(row.Name, row.Value, row.Interest, row.Priority, row.Link, row.Notes, row.DesiredDate));
            if (prepared.Errors.Count > 0) throw new InvalidDataException($"Objetivo '{row.Name}': {string.Join(' ', prepared.Errors.Values.SelectMany(x => x))}");
            var entity = new Objective { Id = Guid.NewGuid(), CreatedAtUnix = new DateTimeOffset(row.CreatedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds(), UpdatedAtUnix = clock.GetUtcNow().ToUnixTimeSeconds() };
            ObjectiveService.Apply(entity, prepared.Value!); db.Objectives.Add(entity); created++;
        }
        return (created, skipped);
    }

    private static Model Parse(string? base64)
    {
        byte[] bytes;
        try { bytes = Convert.FromBase64String(base64 ?? ""); }
        catch (FormatException) { return Empty("O arquivo XLSX ou XLSM não pôde ser lido."); }
        if (bytes.Length is 0 or > MaxBytes) return Empty("O arquivo XLSX ou XLSM deve ter no máximo 10 MB.");
        var preflightError = OpenXmlPreflight.Validate(bytes);
        if (preflightError is not null) return Empty(preflightError);
        try
        {
            using var stream = new MemoryStream(bytes); using var workbook = new XLWorkbook(stream);
            var present = workbook.Worksheets.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!present.Overlaps(["Registros", "Objetivos", "Tipos", "Categorias", "Pagamentos"])) return Empty("A planilha não contém nenhuma aba de dados reconhecida.");
            var errors = new List<string>();
            var types = ReadNames(workbook, "Tipos", errors);
            var payments = ReadNames(workbook, "Pagamentos", errors);
            var categories = ReadCategories(workbook, errors);
            var records = ReadRecords(workbook, errors);
            var objectives = ReadObjectives(workbook, errors);
            Limit(types.Count, 5_000, "Tipos", errors);
            Limit(payments.Count, 5_000, "Pagamentos", errors);
            Limit(categories.Count, 5_000, "Categorias", errors);
            Limit(records.Count, 25_000, "Registros", errors);
            Limit(objectives.Count, 10_000, "Objetivos", errors);
            return new(types, payments, categories, records, objectives, errors, present, Hash(bytes));
        }
        catch (Exception error) when (error is not OutOfMemoryException) { return Empty("O arquivo não é uma planilha XLSX ou XLSM válida."); }
    }

    private static List<string> ReadNames(XLWorkbook workbook, string name, List<string> errors)
    {
        if (!workbook.TryGetWorksheet(name, out var sheet)) return [];
        if (!Header(sheet, ["nome"], errors)) return [];
        var result = new List<string>();
        foreach (var row in Rows(sheet))
        {
            if (HasFormula(row, 1, errors)) continue;
            var value = Text(row.Cell(1));
            if (ValidName(value, name, 80, errors)) result.Add(value);
        }
        return result;
    }

    private static List<CategoryRow> ReadCategories(XLWorkbook workbook, List<string> errors)
    {
        if (!workbook.TryGetWorksheet("Categorias", out var sheet)) return [];
        if (!Header(sheet, ["nome", "categoria_pai"], errors)) return [];
        var result = new List<CategoryRow>();
        foreach (var row in Rows(sheet))
        {
            if (HasFormula(row, 2, errors)) continue;
            var value = new CategoryRow(Text(row.Cell(1)), Text(row.Cell(2)));
            if (ValidName(value.Name, "Categorias", 80, errors) && (value.ParentName.Length == 0 || ValidName(value.ParentName, "Categorias", 80, errors))) result.Add(value);
        }
        return result;
    }

    private static List<RecordRow> ReadRecords(XLWorkbook workbook, List<string> errors)
    {
        if (!workbook.TryGetWorksheet("Registros", out var sheet)) return [];
        string[] headers = ["nome", "movimentacao", "valor", "data", "recorrencia", "parcelas", "tipo", "categoria_pai", "categoria", "pagamento"];
        if (!Header(sheet, headers, errors)) return [];
        var result = new List<RecordRow>();
        foreach (var row in Rows(sheet))
        {
            if (HasFormula(row, headers.Length, errors)) continue;
            var line = row.RowNumber(); var name = Text(row.Cell(1)); var direction = Text(row.Cell(2)); var amount = Money(row.Cell(3), "Registros", line, errors);
            var date = Text(row.Cell(4)); var recurrence = Text(row.Cell(5)); var count = OptionalInt(row.Cell(6), "Registros", line, errors);
            var type = Text(row.Cell(7)); var parent = Text(row.Cell(8)); var category = Text(row.Cell(9)); var payment = Text(row.Cell(10));
            if (name.Length is < 1 or > 120) errors.Add($"Registros, linha {line}: nome inválido.");
            if (direction is not ("entrada" or "saida")) errors.Add($"Registros, linha {line}: movimentação inválida.");
            if (!DateOnly.TryParseExact(date, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) || date.Length != 7) errors.Add($"Registros, linha {line}: data inválida.");
            if (recurrence is not ("unica" or "mensal" or "trimestral" or "semestral" or "anual")) errors.Add($"Registros, linha {line}: recorrência inválida.");
            if (recurrence == "unica" && count != 1 || recurrence != "unica" && count == 1) errors.Add($"Registros, linha {line}: quantidade de parcelas incompatível.");
            if (type.Length is < 1 or > 80 || payment.Length is < 1 or > 80 || parent.Length > 80 || category.Length > 80 || parent.Length > 0 && category.Length == 0) errors.Add($"Registros, linha {line}: classificação inválida.");
            result.Add(new(name, direction, amount, date, recurrence, count, type, parent, category, payment));
        }
        return result;
    }

    private static List<ObjectiveRow> ReadObjectives(XLWorkbook workbook, List<string> errors)
    {
        if (!workbook.TryGetWorksheet("Objetivos", out var sheet)) return [];
        string[] headers = ["nome", "valor_estimado", "interesse", "prioridade", "link", "observacoes", "data_desejada", "data_inclusao"];
        if (!Header(sheet, headers, errors)) return [];
        var result = new List<ObjectiveRow>();
        foreach (var row in Rows(sheet))
        {
            if (HasFormula(row, headers.Length, errors)) continue;
            var line = row.RowNumber();
            if (!int.TryParse(Text(row.Cell(4)), out var priority)) { errors.Add($"Objetivos, linha {line}: prioridade inválida."); priority = 0; }
            if (!DateOnly.TryParseExact(Text(row.Cell(8)), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var created)) { errors.Add($"Objetivos, linha {line}: data de inclusão inválida."); created = DateOnly.FromDateTime(DateTime.UtcNow); }
            var item = new ObjectiveRow(Text(row.Cell(1)), Money(row.Cell(2), "Objetivos", line, errors), Text(row.Cell(3)), priority, Null(Text(row.Cell(5))), Null(Text(row.Cell(6))), Null(Text(row.Cell(7))), created);
            var validation = ObjectiveService.Prepare(new ObjectiveCommand(item.Name, item.Value, item.Interest, item.Priority, item.Link, item.Notes, item.DesiredDate));
            if (validation.Errors.Count > 0) errors.Add($"Objetivos, linha {line}: {string.Join(' ', validation.Errors.Values.SelectMany(x => x))}");
            result.Add(item);
        }
        return result;
    }

    private static IEnumerable<IXLRow> Rows(IXLWorksheet sheet) => sheet.Rows(2, sheet.LastRowUsed()?.RowNumber() ?? 1).Where(x => !x.IsEmpty());
    private static bool HasFormula(IXLRow row, int columns, List<string> errors) { if (!row.Cells(1, columns).Any(cell => cell.HasFormula)) return false; errors.Add($"{row.Worksheet.Name}, linha {row.RowNumber()}: fórmulas não são permitidas nas colunas Seryn."); return true; }
    private static bool Header(IXLWorksheet sheet, string[] expected, List<string> errors) { var actual = Enumerable.Range(1, expected.Length).Select(column => sheet.Cell(1, column).GetString()).ToArray(); if (expected.SequenceEqual(actual, StringComparer.OrdinalIgnoreCase)) return true; errors.Add($"A aba {sheet.Name} possui colunas incompatíveis."); return false; }
    private static int? OptionalInt(IXLCell cell, string sheet, int row, List<string> errors) { var text = Text(cell); if (text.Length == 0) return null; if (int.TryParse(text, out var value) && value > 0) return value; errors.Add($"{sheet}, linha {row}: parcelas inválidas."); return null; }
    private static long Money(IXLCell cell, string sheet, int row, List<string> errors) { if (cell.TryGetValue<decimal>(out var value) && value > 0 && value * 100m <= long.MaxValue) return decimal.ToInt64(decimal.Round(value * 100m, 0, MidpointRounding.AwayFromZero)); errors.Add($"{sheet}, linha {row}: valor inválido."); return 0; }
    private static string Text(IXLCell cell) { var value = cell.GetString().Trim(); return value.Length > 1 && value[0] == '\'' && "=+-@\t\r".Contains(value[1]) ? value[1..] : value; }
    private static bool ValidName(string value, string sheet, int maximum, List<string> errors) { if (value.Length >= 1 && value.Length <= maximum) return true; errors.Add($"{sheet}: nome inválido."); return false; }
    private static string Normalize(string value) => value.Trim().Normalize().ToUpperInvariant();
    private static string? Null(string value) => value.Length == 0 ? null : value;
    private static Model Empty(string error) => new([], [], [], [], [], [error], [], "");
    private static void Limit(int count, int maximum, string sheet, List<string> errors)
    {
        if (count > maximum) errors.Add($"A aba {sheet} deve conter no máximo {maximum:N0} linhas de dados.");
    }
    private static string Hash(byte[] value) => Convert.ToHexStringLower(SHA256.HashData(value));
    private static string HashState(params IEnumerable<string>[] groups)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        foreach (var value in groups.SelectMany(x => x).Order(StringComparer.Ordinal))
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
        writer.Flush();
        return Hash(stream.ToArray());
    }
    private static void Add(List<WorkbookSheetPreview> target, Model model, string sheet, IEnumerable<string> keys, HashSet<string> existing)
    {
        if (!model.PresentSheets.Contains(sheet)) return;
        var seen = new HashSet<string>(existing); var creates = 0; var present = 0; var lines = 0;
        foreach (var key in keys) { lines++; if (seen.Add(key)) creates++; else present++; }
        target.Add(new(sheet, lines, creates, present));
    }
    private static FinancialCategory NewCategory(string name, FinancialCategory? parent) => new() { Id = Guid.NewGuid(), Name = name, NormalizedName = Normalize(name), Parent = parent, ParentId = parent?.Id, ScopeKey = parent?.Id.ToString("N") ?? FinancialCategory.RootScope };
    private static string CategoryKey(string name, string parent) => $"{Normalize(parent)}|{Normalize(name)}";
    private static string RecordKey(RecordRow x) => $"{Normalize(x.Name)}|{x.Direction}|{x.Amount}|{x.Date}|{x.Recurrence}|{x.Count}|{Normalize(x.Type)}|{CategoryKey(x.Category, x.ParentCategory)}|{Normalize(x.Payment)}";
    private static string RecordKey(FinancialRecord x) => $"{Normalize(x.Name)}|{Direction(x.Direction)}|{x.AmountCents}|{x.StartYear:0000}-{x.StartMonth:00}|{Recurrence(x.Recurrence)}|{x.OccurrenceCount}|{Normalize(x.Type.Name)}|{CategoryKey(x.Category?.Name ?? "", x.Category?.Parent?.Name ?? "")}|{Normalize(x.PaymentMethod.Name)}";
    private static string ObjectiveKey(ObjectiveRow x) => $"{Normalize(x.Name)}|{x.Value}|{x.Interest}|{x.Priority}|{x.Link}|{x.Notes}|{x.DesiredDate}|{x.CreatedDate:yyyy-MM-dd}";
    private static string ObjectiveKey(Objective x) => $"{Normalize(x.Name)}|{x.EstimatedValueCents}|{Interest(x.Interest)}|{x.Priority}|{x.Link}|{x.Notes}|{(x.DesiredYear is { } year && x.DesiredMonth is { } month ? $"{year:0000}-{month:00}" : null)}|{DateTimeOffset.FromUnixTimeSeconds(x.CreatedAtUnix):yyyy-MM-dd}";
    private static string Direction(RecordDirection value) => value == RecordDirection.Income ? "entrada" : "saida";
    private static string Recurrence(RecordRecurrence value) => value switch { RecordRecurrence.Once => "unica", RecordRecurrence.Monthly => "mensal", RecordRecurrence.Quarterly => "trimestral", RecordRecurrence.Semiannual => "semestral", _ => "anual" };
    private static string Interest(ObjectiveInterest value) => value switch { ObjectiveInterest.Listed => "listado", ObjectiveInterest.Considered => "considerado", _ => "desejado" };

}
