using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Modules.Records.Domain;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Records.Application;

internal sealed class RecordCommandProcessor(SerynDbContext db)
{
    public async Task<(PreparedRecord? Value, Dictionary<string, string[]> Errors)> Prepare(RecordCommand command)
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

    public static void Apply(FinancialRecord entity, PreparedRecord value)
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

    private static long DivideAndRound(long totalCents, int occurrenceCount)
    {
        var quotient = totalCents / occurrenceCount;
        var remainder = totalCents % occurrenceCount;
        var rounded = quotient + (remainder * 2L >= occurrenceCount ? 1 : 0);
        return Math.Max(1, rounded);
    }

    private static (int Year, int Month)? ParseMonth(string? value)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            value?.Length != 7)
            return null;
        return (date.Year, date.Month);
    }
}

internal sealed record PreparedRecord(
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
