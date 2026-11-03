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
    }
}
