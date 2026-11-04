using Microsoft.EntityFrameworkCore;

namespace Seryn.Server.Persistence;

public sealed class LocalUser
{
    public int Id { get; set; } = 1;
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int FailedAttempts { get; set; }
    public long? LockedUntilUnix { get; set; }
}

public sealed class LoginSession
{
    public string Id { get; set; } = "";
    public long ExpiresAtUnix { get; set; }
}

public sealed class SerynDbContext(DbContextOptions<SerynDbContext> options) : DbContext(options)
{
    public DbSet<LocalUser> Users => Set<LocalUser>();
    public DbSet<LoginSession> Sessions => Set<LoginSession>();
    public DbSet<FinancialType> FinancialTypes => Set<FinancialType>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LocalUser>(user =>
        {
            user.ToTable("local_user", table => table.HasCheckConstraint("single_user", "Id = 1"));
            user.HasKey(x => x.Id);
            user.Property(x => x.Id).ValueGeneratedNever();
            user.Property(x => x.Username).HasMaxLength(64);
        });
        modelBuilder.Entity<LoginSession>(session =>
        {
            session.ToTable("sessions");
            session.HasKey(x => x.Id);
            session.HasIndex(x => x.ExpiresAtUnix);
        });
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

    private static void ConfigureClassification<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> classification, string table)
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
