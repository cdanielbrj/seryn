namespace Seryn.Server.Persistence;

public enum ObjectiveInterest { Listed, Considered, Desired }

public sealed class Objective
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public long EstimatedValueCents { get; set; }
    public ObjectiveInterest Interest { get; set; }
    public int Priority { get; set; }
    public string? Link { get; set; }
    public string? Notes { get; set; }
    public int? DesiredYear { get; set; }
    public int? DesiredMonth { get; set; }
    public long CreatedAtUnix { get; set; }
    public long UpdatedAtUnix { get; set; }
}
