using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Projections.Contracts;
using Seryn.Server.Modules.Projections.Domain;
using Seryn.Server.Modules.Records.Domain;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Projections.Application;

public sealed class AnalysisProjectionService(SerynDbContext db)
{
    private sealed record Source(ProjectionRecord Projection, string Type, string Category, string Payment);
    private sealed record Occurrence(Source Source, YearMonth Month);

    public async Task<(AnalysisProjection? Value, Dictionary<string, string[]>? Errors)> Get(AnalysisQuery query)
    {
        if (!TryParse(query, out var period, out var reference, out var direction, out var primary, out var secondary, out var errors))
            return (null, errors);

        var (start, end) = Range(period, reference);
        var sources = await db.FinancialRecords.AsNoTracking().Select(record => new Source(
            new ProjectionRecord(record.Id, record.Name, record.Direction, record.AmountCents,
                new YearMonth(record.StartYear, record.StartMonth), record.Recurrence, record.OccurrenceCount,
                record.Category == null ? "Sem categoria" : record.Category.Parent == null ? record.Category.Name : record.Category.Parent.Name),
            record.Type.Name,
            record.Category == null ? "Sem categoria" : record.Category.Parent == null ? record.Category.Name : record.Category.Parent.Name,
            record.PaymentMethod.Name)).ToListAsync();
        var byId = sources.ToDictionary(x => x.Projection.Id);
        var occurrences = RecurrenceCalculator.Project(sources.Select(x => x.Projection), start, end)
            .Select(x => new Occurrence(byId[x.Record.Id], x.Month))
            .Where(x => direction == "all"
                || direction == "income" && x.Source.Projection.Direction == RecordDirection.Income
                || direction == "expense" && x.Source.Projection.Direction == RecordDirection.Expense)
            .ToList();

        var incomes = occurrences.Where(x => x.Source.Projection.Direction == RecordDirection.Income).Sum(x => x.Source.Projection.AmountCents);
        var expenses = occurrences.Where(x => x.Source.Projection.Direction == RecordDirection.Expense).Sum(x => x.Source.Projection.AmountCents);
        var basis = incomes + expenses;
        var segments = occurrences.GroupBy(x => Dimension(x.Source, primary), StringComparer.OrdinalIgnoreCase)
            .Select(group => Segment(group.Key, group.ToList(), secondary, basis, end))
            .OrderByDescending(x => x.ValorCentavos).ThenBy(x => x.Nome, StringComparer.OrdinalIgnoreCase).ToList();

        return (new AnalysisProjection(PeriodName(period), reference.ToString(), start.ToString(), end.ToString(), direction, primary, secondary,
            incomes, expenses, incomes - expenses, occurrences.Select(x => x.Source.Projection.Id).Distinct().Count(), occurrences.Count, basis, segments), null);
    }

    private static AnalysisSegmentProjection Segment(string name, List<Occurrence> occurrences, string secondary, long basis, YearMonth periodEnd)
    {
        var amount = occurrences.Sum(x => x.Source.Projection.AmountCents);
        var crosses = secondary == "none" ? [] : occurrences
            .GroupBy(x => Dimension(x.Source, secondary), StringComparer.OrdinalIgnoreCase)
            .Select(group => new AnalysisCrossProjection(Key(group.Key), group.Key, group.Count(), group.Select(x => x.Source.Projection.Id).Distinct().Count(), group.Sum(x => x.Source.Projection.AmountCents)))
            .OrderByDescending(x => x.ValorCentavos).ThenBy(x => x.Nome, StringComparer.OrdinalIgnoreCase).ToList();
        var details = occurrences.GroupBy(x => x.Source.Projection.Id).Select(group =>
        {
            var source = group.First().Source;
            var isInstallmentSeries = source.Projection.Recurrence != RecordRecurrence.Once && source.Projection.OccurrenceCount is >= 2;
            var installmentsThroughPeriod = isInstallmentSeries
                ? RecurrenceCalculator.Count(source.Projection, source.Projection.Start, periodEnd)
                : (int?)null;
            return new AnalysisRecordProjection(source.Projection.Id.ToString(), source.Projection.Name, DirectionName(source.Projection.Direction), source.Type,
                source.Category, source.Payment, group.Count(), isInstallmentSeries ? source.Projection.OccurrenceCount : null,
                installmentsThroughPeriod, group.Sum(x => x.Source.Projection.AmountCents));
        }).OrderByDescending(x => x.ValorCentavos).ThenBy(x => x.Nome, StringComparer.OrdinalIgnoreCase).ToList();
        return new(Key(name), name, occurrences.Count, details.Count, amount,
            basis == 0 ? 0 : (int)Math.Round(amount * 100m / basis, MidpointRounding.AwayFromZero), crosses, details);
    }

    private static string Dimension(Source source, string dimension) => dimension switch
    {
        "category" => source.Category,
        "payment" => source.Payment,
        "type" => source.Type,
        _ => throw new ArgumentOutOfRangeException(nameof(dimension))
    };

    private static bool TryParse(AnalysisQuery query, out string period, out YearMonth reference, out string direction, out string primary, out string secondary, out Dictionary<string, string[]> errors)
    {
        errors = [];
        period = query.Period ?? "";
        direction = query.Direction ?? "all";
        primary = query.Primary ?? "category";
        secondary = query.Secondary ?? "none";
        if (period is not ("month" or "semester" or "year")) errors["period"] = ["Escolha mês, semestre ou ano."];
        if (!DateOnly.TryParseExact(query.Reference, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || query.Reference?.Length != 7)
            errors["reference"] = ["Informe a referência no formato AAAA-MM."];
        if (direction is not ("all" or "income" or "expense")) errors["direction"] = ["Escolha todas, entradas ou saídas."];
        if (primary is not ("category" or "payment" or "type")) errors["primary"] = ["Escolha categoria, pagamento ou tipo."];
        if (secondary is not ("none" or "category" or "payment" or "type") || secondary == primary) errors["secondary"] = ["Escolha uma dimensão secundária diferente da principal."];
        reference = errors.ContainsKey("reference") ? default : new(date.Year, date.Month);
        return errors.Count == 0;
    }

    private static (YearMonth Start, YearMonth End) Range(string period, YearMonth reference) => period switch
    {
        "month" => (reference, reference),
        "semester" => reference.Month <= 6 ? (new(reference.Year, 1), new(reference.Year, 6)) : (new(reference.Year, 7), new(reference.Year, 12)),
        _ => (new(reference.Year, 1), new(reference.Year, 12))
    };
    private static string PeriodName(string period) => period;
    private static string DirectionName(RecordDirection direction) => direction == RecordDirection.Income ? "entrada" : "saida";
    private static string Key(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..12];
}
