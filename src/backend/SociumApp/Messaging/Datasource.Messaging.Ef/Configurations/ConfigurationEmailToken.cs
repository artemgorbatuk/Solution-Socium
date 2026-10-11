using Datasource.Messaging.Ef.Common;
using Datasource.Messaging.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Messaging.Ef.Configurations;

public class ConfigurationEmailToken : IEntityTypeConfiguration<EmailToken>
{
    public void Configure(EntityTypeBuilder<EmailToken> builder)
    {
        builder.ToTable("EmailTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(token => token.UserId)
            .IsRequired();

        builder.Property(token => token.Purpose)
            .IsRequired();

        // SHA-256 в Base64 — 44 символа.
        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(token => token.CreatedAt)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.Property(token => token.UsedAt);

        builder.HasIndex(token => token.TokenHash, "IX_EmailTokens_TokenHash")
            .IsUnique();

        builder.HasIndex(token => new { token.UserId, token.Purpose }, "IX_EmailTokens_UserId_Purpose");
    }
}
