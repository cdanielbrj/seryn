using Microsoft.EntityFrameworkCore;
using Seryn.Server.Persistence.Configurations;

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
        modelBuilder.ConfigureAuth();
        modelBuilder.ConfigureClassifications();
        modelBuilder.ConfigureFinancialRecords();
    }
}
