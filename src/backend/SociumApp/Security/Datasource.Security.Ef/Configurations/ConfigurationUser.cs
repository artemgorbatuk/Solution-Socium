using Datasource.Security.Ef.Common;
using Datasource.Security.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Security.Ef.Configurations;

public class ConfigurationUser : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        // Хранится в нижнем регистре — уникальность без учёта регистра.
        builder.Property(user => user.Login)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.IsEmailConfirmed)
            .IsRequired();

        builder.Property(user => user.IsDeleted)
            .IsRequired();

        builder.HasIndex(user => user.Login, "IX_Users_Login")
            .IsUnique();

        builder.HasIndex(user => user.Email, "IX_Users_Email")
            .IsUnique();
    }
}
