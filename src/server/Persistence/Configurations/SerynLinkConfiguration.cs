using Microsoft.EntityFrameworkCore;

namespace Seryn.Server.Persistence.Configurations;

internal static class SerynLinkConfiguration
{
    public static void ConfigureSerynLink(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiToken>(token =>
        {
            token.ToTable("api_tokens");
            token.HasKey(x => x.Id);
            token.Property(x => x.Name).HasMaxLength(80);
            token.Property(x => x.TokenHash).HasMaxLength(64);
            token.Property(x => x.DisplayPrefix).HasMaxLength(24);
            token.Property(x => x.Scopes).HasMaxLength(64);
            token.HasIndex(x => x.TokenHash).IsUnique();
            token.HasIndex(x => x.RevokedAtUnix);
        });
        modelBuilder.Entity<IdempotencyRecord>(record =>
        {
            record.ToTable("idempotency_records");
            record.HasKey(x => x.Id);
            record.Property(x => x.Key).HasMaxLength(128);
            record.Property(x => x.RequestHash).HasMaxLength(64);
            record.Property(x => x.ContentType).HasMaxLength(128);
            record.Property(x => x.ResponseHeadersJson).HasMaxLength(2048);
            record.HasIndex(x => new { x.ApiTokenId, x.Key }).IsUnique();
            record.HasIndex(x => x.ExpiresAtUnix);
            record.HasOne<ApiToken>().WithMany().HasForeignKey(x => x.ApiTokenId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
