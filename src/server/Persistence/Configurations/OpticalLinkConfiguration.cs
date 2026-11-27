using Microsoft.EntityFrameworkCore;

namespace Seryn.Server.Persistence.Configurations;

internal static class OpticalLinkConfiguration
{
    public static void ConfigureOpticalLink(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OpticalLinkConnection>(connection =>
        {
            connection.ToTable("optical_link_connections", table =>
            {
                table.HasCheckConstraint("optical_link_provider", "Provider = 'openrouter'");
                table.HasCheckConstraint("optical_link_default", "IsDefault IN (0, 1)");
            });
            connection.HasKey(x => x.Id);
            connection.Property(x => x.Name).HasMaxLength(80);
            connection.Property(x => x.Provider).HasMaxLength(32);
            connection.Property(x => x.Model).HasMaxLength(160);
            connection.Property(x => x.ProtectedApiKey).HasMaxLength(4096);
            connection.Property(x => x.KeyHint).HasMaxLength(16);
            connection.HasIndex(x => x.Name).IsUnique();
            connection.HasIndex(x => x.IsDefault).HasFilter("IsDefault = 1").IsUnique();
        });
    }
}
