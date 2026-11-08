using Seryn.Server.Modules.Records.Domain;

namespace Seryn.Server.Modules.Projections.Domain;

public sealed record ProjectionRecord(
    Guid Id,
    string Name,
    RecordDirection Direction,
    long AmountCents,
    YearMonth Start,
    RecordRecurrence Recurrence,
    int? OccurrenceCount,
    string CategoryName);

public sealed record ProjectedOccurrence(ProjectionRecord Record, YearMonth Month);

/// <summary>Pure month-based recurrence calculation with no HTTP, EF Core, or persistence dependency.</summary>
public static class RecurrenceCalculator
{
    public static IEnumerable<ProjectedOccurrence> Project(
        IEnumerable<ProjectionRecord> records,
        YearMonth start,
        YearMonth end)
    {
        foreach (var record in records)
        {
            var interval = IntervalInMonths(record.Recurrence);
            if (end.DistanceFrom(record.Start) < 0)
                continue;

            var firstIndex = Math.Max(0, CeilingDivide(start.DistanceFrom(record.Start), interval));
            var lastIndex = end.DistanceFrom(record.Start) / interval;
            if (record.OccurrenceCount is { } count)
                lastIndex = Math.Min(lastIndex, count - 1);
            if (lastIndex < firstIndex)
                continue;

            for (var index = firstIndex; index <= lastIndex; index++)
                yield return new(record, record.Start.AddMonths(index * interval));
        }
    }

    private static int IntervalInMonths(RecordRecurrence recurrence) => recurrence switch
    {
        RecordRecurrence.Once => 1,
        RecordRecurrence.Monthly => 1,
        RecordRecurrence.Quarterly => 3,
        RecordRecurrence.Semiannual => 6,
        RecordRecurrence.Annual => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(recurrence))
    };

    private static int CeilingDivide(int value, int divisor) =>
        value <= 0 ? 0 : (value + divisor - 1) / divisor;
}

public readonly record struct YearMonth
{
    public YearMonth(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9999);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        Year = year;
        Month = month;
    }

    public int Year { get; }
    public int Month { get; }

    public YearMonth AddMonths(int amount)
    {
        var zeroBased = checked((Year - 1) * 12 + Month - 1 + amount);
        if (zeroBased is < 0 or > 119_987)
            throw new ArgumentOutOfRangeException(nameof(amount));
        return new(zeroBased / 12 + 1, zeroBased % 12 + 1);
    }

    public int DistanceFrom(YearMonth other) => (Year - other.Year) * 12 + Month - other.Month;
    public override string ToString() => $"{Year:0000}-{Month:00}";
}
