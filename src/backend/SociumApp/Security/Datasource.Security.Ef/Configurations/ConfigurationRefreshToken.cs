using Datasource.Security.Ef.Common;
using Datasource.Security.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Security.Ef.Configurations;

public class ConfigurationRefreshToken : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(token => token.UserId)
            .IsRequired();

        // SHA-256 в Base64 — 44 символа.
        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(token => token.IsPersistent)
            .IsRequired();

        builder.Property(token => token.CreatedAt)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.Property(token => token.RevokedAt);

        builder.Property(token => token.ReplacedByTokenId);

        builder.HasIndex(token => token.TokenHash, "IX_RefreshTokens_TokenHash")
            .IsUnique();

        builder.HasIndex(token => token.UserId, "IX_RefreshTokens_UserId");

        // Пользователь удаляется мягко (IsDeleted), каскад срабатывает только при физическом удалении.
        builder.HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
