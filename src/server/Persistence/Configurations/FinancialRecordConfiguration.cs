using Microsoft.EntityFrameworkCore;

namespace Seryn.Server.Persistence.Configurations;

internal static class FinancialRecordConfiguration
{
    public static void ConfigureFinancialRecords(this ModelBuilder modelBuilder)
    {
        var record = modelBuilder.Entity<FinancialRecord>();
        record.ToTable("financial_records", table =>
        {
            table.HasCheckConstraint("record_amount_positive", "AmountCents > 0");
            table.HasCheckConstraint("record_start_month", "StartMonth >= 1 AND StartMonth <= 12");
            table.HasCheckConstraint("record_start_year", "StartYear >= 1 AND StartYear <= 9999");
            table.HasCheckConstraint("record_occurrence_count", "OccurrenceCount IS NULL OR OccurrenceCount >= 1");
        });
        record.HasKey(x => x.Id);
        record.Property(x => x.Id).ValueGeneratedNever();
        record.Property(x => x.Name).HasMaxLength(120);
        record.Property(x => x.Direction).HasConversion<string>().HasMaxLength(16);
        record.Property(x => x.Recurrence).HasConversion<string>().HasMaxLength(16);
        record.HasIndex(x => new { x.StartYear, x.StartMonth });
        record.HasIndex(x => x.TypeId);
        record.HasIndex(x => x.CategoryId);
        record.HasIndex(x => x.PaymentMethodId);
        record.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId).OnDelete(DeleteBehavior.Restrict);
        record.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        record.HasOne(x => x.PaymentMethod).WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
    }
}
