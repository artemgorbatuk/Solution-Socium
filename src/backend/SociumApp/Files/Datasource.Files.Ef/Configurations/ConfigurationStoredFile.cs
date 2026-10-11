using Datasource.Files.Ef.Common;
using Datasource.Files.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Files.Ef.Configurations;

public class ConfigurationStoredFile : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        builder.HasKey(file => file.Id);

        builder.Property(file => file.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(file => file.OriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(file => file.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(file => file.SizeBytes)
            .IsRequired();

        builder.Property(file => file.StorageKey)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(file => file.UploadedByUserId)
            .IsRequired();

        builder.Property(file => file.CreatedAt)
            .IsRequired();

        builder.Property(file => file.OwnerType);

        builder.Property(file => file.OwnerId);

        builder.HasIndex(file => file.StorageKey, "IX_StoredFiles_StorageKey")
            .IsUnique();

        builder.HasIndex(file => new { file.OwnerType, file.OwnerId }, "IX_StoredFiles_OwnerType_OwnerId");

        builder.HasIndex(file => file.UploadedByUserId, "IX_StoredFiles_UploadedByUserId");
    }
}
