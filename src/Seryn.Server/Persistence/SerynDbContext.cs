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
