using Seryn.Server.Persistence;

namespace Seryn.Server.Projections;

/// <summary>Pure, month-based recurrence calculation used by every projection query.</summary>
public static class RecurrenceProjection
{
    public static IEnumerable<ProjectedOccurrence> Project(IEnumerable<FinancialRecord> records, YearMonth start, YearMonth end)
    {
        foreach (var record in records)
        {
            var first = new YearMonth(record.StartYear, record.StartMonth);
            var interval = IntervalInMonths(record.Recurrence);
            if (end.DistanceFrom(first) < 0)
                continue;
            var firstIndex = Math.Max(0, CeilingDivide(start.DistanceFrom(first), interval));
            var lastIndex = end.DistanceFrom(first) / interval;
            if (lastIndex < firstIndex)
                continue;

            if (record.OccurrenceCount is { } count)
                lastIndex = Math.Min(lastIndex, count - 1);

            for (var index = firstIndex; index <= lastIndex; index++)
            {
                var month = first.AddMonths(index * interval);
                yield return new(record, month);
            }
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

    private static int CeilingDivide(int value, int divisor) => value <= 0 ? 0 : (value + divisor - 1) / divisor;
}

public readonly record struct YearMonth(int Year, int Month)
{
    public YearMonth AddMonths(int amount)
    {
        var zeroBased = checked(Year * 12 + Month - 1 + amount);
        return new(zeroBased / 12, zeroBased % 12 + 1);
    }

    public int DistanceFrom(YearMonth other) => (Year - other.Year) * 12 + Month - other.Month;
    public override string ToString() => $"{Year:0000}-{Month:00}";
}

public sealed record ProjectedOccurrence(FinancialRecord Record, YearMonth Month);
