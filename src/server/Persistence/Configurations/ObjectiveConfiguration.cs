using Microsoft.EntityFrameworkCore;

namespace Seryn.Server.Persistence.Configurations;

internal static class ObjectiveConfiguration
{
    public static void ConfigureObjectives(this ModelBuilder modelBuilder)
    {
        var objective = modelBuilder.Entity<Objective>();
        objective.ToTable("objectives", table =>
        {
            table.HasCheckConstraint("objective_value_positive", "EstimatedValueCents > 0");
            table.HasCheckConstraint("objective_priority_range", "Priority >= -3 AND Priority <= 3");
            table.HasCheckConstraint("objective_desired_month", "(DesiredYear IS NULL AND DesiredMonth IS NULL) OR (DesiredYear >= 1 AND DesiredYear <= 9999 AND DesiredMonth >= 1 AND DesiredMonth <= 12)");
        });
        objective.HasKey(x => x.Id);
        objective.Property(x => x.Id).ValueGeneratedNever();
        objective.Property(x => x.Name).HasMaxLength(120);
        objective.Property(x => x.Interest).HasConversion<string>().HasMaxLength(16);
        objective.Property(x => x.Link).HasMaxLength(2048);
        objective.Property(x => x.Notes).HasMaxLength(2000);
        objective.HasIndex(x => new { x.Interest, x.Priority });
    }
}
