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
        modelBuilder.Entity<WorkbookImportPreview>(preview =>
        {
            preview.ToTable("workbook_import_previews");
            preview.HasKey(x => x.Id);
            preview.Property(x => x.SessionId).HasMaxLength(32);
            preview.Property(x => x.WorkbookHash).HasMaxLength(64);
            preview.Property(x => x.CurrentDataHash).HasMaxLength(64);
            preview.HasIndex(x => x.ExpiresAtUnix);
            preview.HasOne<LoginSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
