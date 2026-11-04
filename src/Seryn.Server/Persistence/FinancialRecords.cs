namespace Seryn.Server.Persistence;

public enum RecordDirection
{
    Income,
    Expense
}

public enum RecordRecurrence
{
    Once,
    Monthly,
    Quarterly,
    Semiannual,
    Annual
}

public sealed class FinancialRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public RecordDirection Direction { get; set; }
    public long AmountCents { get; set; }
    public int StartYear { get; set; }
    public int StartMonth { get; set; }
    public RecordRecurrence Recurrence { get; set; }
    public int? OccurrenceCount { get; set; }
    public Guid TypeId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public long CreatedAtUnix { get; set; }
    public long UpdatedAtUnix { get; set; }
    public FinancialType Type { get; set; } = null!;
    public FinancialCategory? Category { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;
}
