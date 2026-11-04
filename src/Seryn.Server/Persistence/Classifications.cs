namespace Seryn.Server.Persistence;

public interface IClassification
{
    Guid Id { get; set; }
    string Name { get; set; }
    string NormalizedName { get; set; }
}

public sealed class FinancialType : IClassification
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
}

public sealed class PaymentMethod : IClassification
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
}

public sealed class FinancialCategory : IClassification
{
    public const string RootScope = "ROOT";
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public Guid? ParentId { get; set; }
    public string ScopeKey { get; set; } = RootScope;
    public FinancialCategory? Parent { get; set; }
    public ICollection<FinancialCategory> Children { get; set; } = [];
}
