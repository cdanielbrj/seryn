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
    public long CreatedAtUnix { get; set; }
    public long LastUsedAtUnix { get; set; }
    public long ExpiresAtUnix { get; set; }
}

public sealed class WorkbookImportPreview
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = "";
    public string WorkbookHash { get; set; } = "";
    public string CurrentDataHash { get; set; } = "";
    public bool ReplaceData { get; set; }
    public long ExpiresAtUnix { get; set; }
    public long? ConsumedAtUnix { get; set; }
}

public sealed class ApiToken
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string DisplayPrefix { get; set; } = "";
    public string Scopes { get; set; } = "";
    public long CreatedAtUnix { get; set; }
    public long? ExpiresAtUnix { get; set; }
    public long? LastUsedAtUnix { get; set; }
    public long? RevokedAtUnix { get; set; }
}

public sealed class OpticalLinkConnection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public string ProtectedApiKey { get; set; } = "";
    public string KeyHint { get; set; } = "";
    public bool IsDefault { get; set; }
    public long CreatedAtUnix { get; set; }
    public long UpdatedAtUnix { get; set; }
}

public sealed class IdempotencyRecord
{
    public long Id { get; set; }
    public Guid ApiTokenId { get; set; }
    public string Key { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = "application/json";
    public string ResponseHeadersJson { get; set; } = "{}";
    public byte[] ResponseBody { get; set; } = [];
    public long ExpiresAtUnix { get; set; }
}

public sealed class SerynDbContext(DbContextOptions<SerynDbContext> options) : DbContext(options)
{
    public DbSet<LocalUser> Users => Set<LocalUser>();
    public DbSet<LoginSession> Sessions => Set<LoginSession>();
    public DbSet<WorkbookImportPreview> WorkbookImportPreviews => Set<WorkbookImportPreview>();
    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();
    public DbSet<OpticalLinkConnection> OpticalLinkConnections => Set<OpticalLinkConnection>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<FinancialType> FinancialTypes => Set<FinancialType>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();
    public DbSet<Objective> Objectives => Set<Objective>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureAuth();
        modelBuilder.ConfigureSerynLink();
        modelBuilder.ConfigureOpticalLink();
        modelBuilder.ConfigureClassifications();
        modelBuilder.ConfigureFinancialRecords();
        modelBuilder.ConfigureObjectives();
    }
}
