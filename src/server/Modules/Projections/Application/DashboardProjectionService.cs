using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Projections.Contracts;
using Seryn.Server.Modules.Projections.Domain;
using Seryn.Server.Modules.Records.Domain;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Projections.Application;

public sealed class DashboardProjectionService(SerynDbContext db)
{
    public async Task<(DashboardProjection? Value, Dictionary<string, string[]>? Errors)> Get(ProjectionQuery query)
    {
        if (!TryParse(query, out var period, out var reference, out var errors))
            return (null, errors);

        var (start, end) = SelectedRange(period, reference);
        var (chartStart, chartEnd) = period == ProjectionPeriod.Month
            ? (AddWithinCalendar(reference, -3), AddWithinCalendar(reference, 3))
            : (start, end);
        var records = await db.FinancialRecords.AsNoTracking()
            .Select(record => new ProjectionRecord(
                record.Id,
                record.Name,
                record.Direction,
                record.AmountCents,
                new YearMonth(record.StartYear, record.StartMonth),
                record.Recurrence,
                record.OccurrenceCount,
                record.Category == null
                    ? "Sem categoria"
                    : record.Category.Parent == null ? record.Category.Name : record.Category.Parent.Name))
            .ToListAsync();
        var selectedOccurrences = RecurrenceCalculator.Project(records, start, end).ToList();
        var chartOccurrences = RecurrenceCalculator.Project(records, chartStart, chartEnd).ToList();
        var incomes = selectedOccurrences.Where(x => x.Record.Direction == RecordDirection.Income).Sum(x => x.Record.AmountCents);
        var expenses = selectedOccurrences.Where(x => x.Record.Direction == RecordDirection.Expense).Sum(x => x.Record.AmountCents);

        var chart = new List<ProjectionBucket>();
        long cumulative = 0;
        for (var month = chartStart; month.DistanceFrom(chartEnd) <= 0; month = month.AddMonths(1))
        {
            var occurrences = chartOccurrences.Where(x => x.Month == month).ToList();
            var monthIncome = occurrences.Where(x => x.Record.Direction == RecordDirection.Income).Sum(x => x.Record.AmountCents);
            var monthExpense = occurrences.Where(x => x.Record.Direction == RecordDirection.Expense).Sum(x => x.Record.AmountCents);
            cumulative += monthIncome - monthExpense;
            chart.Add(new(month.ToString(), monthIncome, monthExpense, cumulative, month == reference));
        }

        var categories = selectedOccurrences.Where(x => x.Record.Direction == RecordDirection.Expense)
            .GroupBy(x => x.Record.CategoryName)
            .Select(group => new { group.Key, Amount = group.Sum(x => x.Record.AmountCents) })
            .OrderByDescending(x => x.Amount).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new CategoryProjection(x.Key, x.Amount, expenses == 0 ? 0 : (int)Math.Round(x.Amount * 100m / expenses, MidpointRounding.AwayFromZero)))
            .ToList();

        return (new(PeriodName(period), reference.ToString(), start.ToString(), end.ToString(), incomes, expenses, incomes - expenses,
            chartStart.ToString(), chartEnd.ToString(), chart, categories,
            Top(selectedOccurrences, RecordDirection.Income), Top(selectedOccurrences, RecordDirection.Expense)), null);
    }

    private static List<TopRecordProjection> Top(IEnumerable<ProjectedOccurrence> occurrences, RecordDirection direction) => occurrences
        .Where(x => x.Record.Direction == direction)
        .GroupBy(x => x.Record.Id)
        .Select(group => new TopRecordProjection(group.Key.ToString(), group.First().Record.Name, group.First().Record.CategoryName, group.Count(), group.Sum(x => x.Record.AmountCents)))
        .OrderByDescending(x => x.ValorCentavos).ThenBy(x => x.Nome, StringComparer.OrdinalIgnoreCase).Take(3).ToList();

    private static bool TryParse(ProjectionQuery query, out ProjectionPeriod period, out YearMonth reference, out Dictionary<string, string[]> errors)
    {
        errors = new();
        period = query.Period switch { "month" => ProjectionPeriod.Month, "semester" => ProjectionPeriod.Semester, "year" => ProjectionPeriod.Year, _ => ProjectionPeriod.Month };
        if (query.Period is not ("month" or "semester" or "year")) errors["period"] = ["Escolha mês, semestre ou ano."];
        if (!DateOnly.TryParseExact(query.Reference, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || query.Reference?.Length != 7)
            errors["reference"] = ["Informe a referência no formato AAAA-MM."];
        reference = errors.ContainsKey("reference") ? default : new(date.Year, date.Month);
        return errors.Count == 0;
    }

    private static (YearMonth Start, YearMonth End) SelectedRange(ProjectionPeriod period, YearMonth reference) => period switch
    {
        ProjectionPeriod.Month => (reference, reference),
        ProjectionPeriod.Semester => reference.Month <= 6 ? (new(reference.Year, 1), new(reference.Year, 6)) : (new(reference.Year, 7), new(reference.Year, 12)),
        _ => (new(reference.Year, 1), new(reference.Year, 12))
    };

    private static string PeriodName(ProjectionPeriod period) => period switch { ProjectionPeriod.Month => "month", ProjectionPeriod.Semester => "semester", _ => "year" };
    private static YearMonth AddWithinCalendar(YearMonth value, int months)
    {
        var index = Math.Clamp(value.Year * 12 + value.Month - 1 + months, 12, 119_999);
        return new(index / 12, index % 12 + 1);
    }
    private enum ProjectionPeriod { Month, Semester, Year }
}
