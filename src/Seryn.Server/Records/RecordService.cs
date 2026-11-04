using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Persistence;

namespace Seryn.Server.Records;

public sealed record RecordCommand(
    string? Nome,
    string? Movimentacao,
    Guid TipoId,
    Guid? CategoriaId,
    Guid PagamentoId,
    long ValorCentavos,
    string? Data,
    string? Recorrencia,
    string? Ocorrencia,
    int? Parcelas,
    string? ModoValor);

public sealed record RecordItem(
    Guid Id,
    string Nome,
    string Movimentacao,
    Guid TipoId,
    string TipoNome,
    Guid? CategoriaId,
    string? CategoriaNome,
    Guid PagamentoId,
    string PagamentoNome,
    long ValorCentavos,
    string Data,
    string Recorrencia,
    string Ocorrencia,
    int? Parcelas);

public enum RecordFailure
{
    None,
    Invalid,
    NotFound
}

public sealed record RecordResult(RecordItem? Item, RecordFailure Failure, Dictionary<string, string[]>? Errors = null);

public sealed class RecordService(SerynDbContext db, TimeProvider clock)
{
    public async Task<List<RecordItem>> List()
    {
        var records = await Query().OrderByDescending(x => x.StartYear).ThenByDescending(x => x.StartMonth)
            .ThenBy(x => x.Name).ToListAsync();
        return records.Select(ToItem).ToList();
    }

    public async Task<RecordItem?> Get(Guid id)
    {
        var entity = await Query().SingleOrDefaultAsync(x => x.Id == id);
        return entity is null ? null : ToItem(entity);
    }

    public async Task<RecordResult> Create(RecordCommand command)
    {
        var prepared = await Prepare(command);
        if (prepared.Errors.Count > 0)
            return new(null, RecordFailure.Invalid, prepared.Errors);

        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var entity = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            CreatedAtUnix = now,
            UpdatedAtUnix = now
        };
        Apply(entity, prepared.Value!);
        db.FinancialRecords.Add(entity);
        await db.SaveChangesAsync();
        return new(await Get(entity.Id), RecordFailure.None);
    }

    public async Task<RecordResult> Update(Guid id, RecordCommand command)
    {
        var entity = await db.FinancialRecords.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return new(null, RecordFailure.NotFound);

        var prepared = await Prepare(command);
        if (prepared.Errors.Count > 0)
            return new(null, RecordFailure.Invalid, prepared.Errors);

        Apply(entity, prepared.Value!);
        entity.UpdatedAtUnix = clock.GetUtcNow().ToUnixTimeSeconds();
        await db.SaveChangesAsync();
        return new(await Get(entity.Id), RecordFailure.None);
    }

    public async Task<RecordFailure> Delete(Guid id)
    {
        var entity = await db.FinancialRecords.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return RecordFailure.NotFound;
        db.FinancialRecords.Remove(entity);
        await db.SaveChangesAsync();
        return RecordFailure.None;
    }

    private async Task<(PreparedRecord? Value, Dictionary<string, string[]> Errors)> Prepare(RecordCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        var name = command.Nome?.Trim().Normalize(NormalizationForm.FormC) ?? "";
        if (name.Length is < 1 or > 120)
            errors["nome"] = ["Informe um nome entre 1 e 120 caracteres."];

        var direction = command.Movimentacao switch
        {
            "entrada" => RecordDirection.Income,
            "saida" => RecordDirection.Expense,
            _ => (RecordDirection?)null
        };
        if (direction is null)
            errors["movimentacao"] = ["Escolha entrada ou saída."];

        if (command.ValorCentavos <= 0)
            errors["valorCentavos"] = ["Informe um valor maior que zero."];

        var date = ParseMonth(command.Data);
        if (date is null)
            errors["data"] = ["Informe uma data válida no formato AAAA-MM."];

        var recurrence = command.Recorrencia switch
        {
            "unica" => RecordRecurrence.Once,
            "mensal" => RecordRecurrence.Monthly,
            "trimestral" => RecordRecurrence.Quarterly,
            "semestral" => RecordRecurrence.Semiannual,
            "anual" => RecordRecurrence.Annual,
            _ => (RecordRecurrence?)null
        };
        if (recurrence is null)
            errors["recorrencia"] = ["Escolha uma recorrência válida."];

        int? occurrenceCount = null;
        if (recurrence == RecordRecurrence.Once)
        {
            occurrenceCount = 1;
        }
        else if (recurrence is not null)
        {
            if (command.Ocorrencia == "indefinida")
            {
                occurrenceCount = null;
            }
            else if (command.Ocorrencia == "definida")
            {
                if (command.Parcelas is null or < 2)
                    errors["parcelas"] = ["Informe pelo menos 2 parcelas."];
                else
                    occurrenceCount = command.Parcelas;
            }
            else
            {
                errors["ocorrencia"] = ["Escolha ocorrência definida ou indefinida."];
            }
        }

        var valueMode = command.ModoValor ?? "por_ocorrencia";
        if (valueMode is not ("por_ocorrencia" or "total"))
            errors["modoValor"] = ["Escolha valor por ocorrência ou valor total."];
        if (valueMode == "total" && (recurrence == RecordRecurrence.Once || occurrenceCount is null))
            errors["modoValor"] = ["O valor total só pode ser usado em uma ocorrência definida."];

        if (command.TipoId == Guid.Empty || !await db.FinancialTypes.AnyAsync(x => x.Id == command.TipoId))
            errors["tipoId"] = ["Escolha um tipo válido."];
        if (command.PagamentoId == Guid.Empty || !await db.PaymentMethods.AnyAsync(x => x.Id == command.PagamentoId))
            errors["pagamentoId"] = ["Escolha um pagamento válido."];
        if (command.CategoriaId is not null && !await db.FinancialCategories.AnyAsync(x => x.Id == command.CategoriaId))
            errors["categoriaId"] = ["Escolha uma categoria válida."];

        if (errors.Count > 0 || direction is null || date is null || recurrence is null)
            return (null, errors);
        var amountCents = valueMode == "total"
            ? DivideAndRound(command.ValorCentavos, occurrenceCount!.Value)
            : command.ValorCentavos;
        return (new(name, direction.Value, amountCents, date.Value.Year, date.Value.Month,
            recurrence.Value, occurrenceCount, command.TipoId, command.CategoriaId, command.PagamentoId), errors);
    }

    private static long DivideAndRound(long totalCents, int occurrenceCount)
    {
        var quotient = totalCents / occurrenceCount;
        var remainder = totalCents % occurrenceCount;
        var rounded = quotient + (remainder * 2L >= occurrenceCount ? 1 : 0);
        return Math.Max(1, rounded);
    }

    private IQueryable<FinancialRecord> Query() => db.FinancialRecords.AsNoTracking()
        .Include(x => x.Type).Include(x => x.Category).Include(x => x.PaymentMethod);

    private static (int Year, int Month)? ParseMonth(string? value)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            value?.Length != 7)
            return null;
        return (date.Year, date.Month);
    }

    private static void Apply(FinancialRecord entity, PreparedRecord value)
    {
        entity.Name = value.Name;
        entity.Direction = value.Direction;
        entity.AmountCents = value.AmountCents;
        entity.StartYear = value.StartYear;
        entity.StartMonth = value.StartMonth;
        entity.Recurrence = value.Recurrence;
        entity.OccurrenceCount = value.OccurrenceCount;
        entity.TypeId = value.TypeId;
        entity.CategoryId = value.CategoryId;
        entity.PaymentMethodId = value.PaymentMethodId;
    }

    private static RecordItem ToItem(FinancialRecord entity) => new(
        entity.Id,
        entity.Name,
        entity.Direction == RecordDirection.Income ? "entrada" : "saida",
        entity.TypeId,
        entity.Type.Name,
        entity.CategoryId,
        entity.Category?.Name,
        entity.PaymentMethodId,
        entity.PaymentMethod.Name,
        entity.AmountCents,
        $"{entity.StartYear:0000}-{entity.StartMonth:00}",
        entity.Recurrence switch
        {
            RecordRecurrence.Once => "unica",
            RecordRecurrence.Monthly => "mensal",
            RecordRecurrence.Quarterly => "trimestral",
            RecordRecurrence.Semiannual => "semestral",
            _ => "anual"
        },
        entity.OccurrenceCount is null ? "indefinida" : "definida",
        entity.OccurrenceCount);

    private sealed record PreparedRecord(
        string Name,
        RecordDirection Direction,
        long AmountCents,
        int StartYear,
        int StartMonth,
        RecordRecurrence Recurrence,
        int? OccurrenceCount,
        Guid TypeId,
        Guid? CategoryId,
        Guid PaymentMethodId);
}
