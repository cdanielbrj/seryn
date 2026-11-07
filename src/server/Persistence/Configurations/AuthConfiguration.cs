using Microsoft.EntityFrameworkCore;

namespace Seryn.Server.Persistence.Configurations;

internal static class AuthConfiguration
{
    public static void ConfigureAuth(this ModelBuilder modelBuilder)
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
