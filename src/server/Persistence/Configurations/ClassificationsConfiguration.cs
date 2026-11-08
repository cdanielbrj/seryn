using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Seryn.Server.Persistence.Configurations;

internal static class ClassificationsConfiguration
{
    public static void ConfigureClassifications(this ModelBuilder modelBuilder)
    {
        ConfigureClassification(modelBuilder.Entity<FinancialType>(), "financial_types");
        ConfigureClassification(modelBuilder.Entity<PaymentMethod>(), "payment_methods");

        var category = modelBuilder.Entity<FinancialCategory>();
        category.ToTable("financial_categories");
        category.HasKey(x => x.Id);
        category.Property(x => x.Id).ValueGeneratedNever();
        category.Property(x => x.Name).HasMaxLength(80);
        category.Property(x => x.NormalizedName).HasMaxLength(80);
        category.Property(x => x.ScopeKey).HasMaxLength(32).HasDefaultValue(FinancialCategory.RootScope);
        category.HasIndex(x => new { x.ScopeKey, x.NormalizedName }).IsUnique();
        category.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureClassification<T>(EntityTypeBuilder<T> classification, string table)
        where T : class, IClassification
    {
        classification.ToTable(table);
        classification.HasKey(x => x.Id);
        classification.Property(x => x.Id).ValueGeneratedNever();
        classification.Property(x => x.Name).HasMaxLength(80);
        classification.Property(x => x.NormalizedName).HasMaxLength(80);
        classification.HasIndex(x => x.NormalizedName).IsUnique();
    }
}
