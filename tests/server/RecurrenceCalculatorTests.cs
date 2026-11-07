using Seryn.Server.Modules.Projections.Domain;
using Seryn.Server.Modules.Records.Domain;

namespace Seryn.Tests;

public sealed class RecurrenceCalculatorTests
{
    [Fact]
    public void Finite_monthly_series_is_projected_without_persistence_or_http()
    {
        var record = Record(RecordRecurrence.Monthly, count: 10);

        var firstSemester = RecurrenceCalculator.Project(
            [record], new YearMonth(2027, 1), new YearMonth(2027, 6)).ToList();
        var secondSemester = RecurrenceCalculator.Project(
            [record], new YearMonth(2027, 7), new YearMonth(2027, 12)).ToList();

        Assert.Equal(6, firstSemester.Count);
        Assert.Equal(4, secondSemester.Count);
        Assert.Equal(new YearMonth(2027, 10), secondSemester[^1].Month);
    }

    [Theory]
    [InlineData(RecordRecurrence.Once, 1)]
    [InlineData(RecordRecurrence.Monthly, 1)]
    [InlineData(RecordRecurrence.Quarterly, 3)]
    [InlineData(RecordRecurrence.Semiannual, 6)]
    [InlineData(RecordRecurrence.Annual, 12)]
    public void Recurrence_frequency_has_the_expected_month_interval(
        RecordRecurrence recurrence,
        int expectedInterval)
    {
        var occurrences = RecurrenceCalculator.Project(
            [Record(recurrence, count: recurrence == RecordRecurrence.Once ? 1 : 2)],
            new YearMonth(2027, 1),
            new YearMonth(2028, 12)).ToList();

        Assert.Equal(new YearMonth(2027, 1), occurrences[0].Month);
        if (occurrences.Count > 1)
            Assert.Equal(new YearMonth(2027, 1).AddMonths(expectedInterval), occurrences[1].Month);
    }

    [Fact]
    public void Projection_excludes_occurrences_before_start_and_after_finite_end()
    {
        var record = Record(RecordRecurrence.Monthly, count: 2);

        Assert.Empty(RecurrenceCalculator.Project(
            [record], new YearMonth(2026, 1), new YearMonth(2026, 12)));
        Assert.Empty(RecurrenceCalculator.Project(
            [record], new YearMonth(2027, 3), new YearMonth(2027, 12)));
    }

    [Fact]
    public void YearMonth_rejects_invalid_civil_months()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new YearMonth(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new YearMonth(2027, 13));
        Assert.Throws<ArgumentOutOfRangeException>(() => new YearMonth(1, 1).AddMonths(-1));
    }

    private static ProjectionRecord Record(RecordRecurrence recurrence, int? count) => new(
        Guid.Parse("019a5eef-f9fd-7bca-a60c-f820fd862bd8"),
        "Test record",
        RecordDirection.Expense,
        75_000,
        new YearMonth(2027, 1),
        recurrence,
        count,
        "Test category");
}
