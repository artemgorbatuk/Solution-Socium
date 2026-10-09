using Datasource.Socium.Ef.Common;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Socium.Ef.Configurations;

public class ConfigurationRecipient : IEntityTypeConfiguration<Recipient>
{
    public void Configure(EntityTypeBuilder<Recipient> builder)
    {
        builder.ToTable("Recipients");

        builder.HasKey(recipient => recipient.Id);

        builder.Property(recipient => recipient.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(recipient => recipient.MessageId)
            .IsRequired();

        builder.HasIndex(recipient => new { recipient.MessageId, recipient.UserId }, "IX_Recipients_MessageId_UserId")
            .IsUnique();

        builder.HasIndex(recipient => recipient.UserId, "IX_Recipients_UserId");

        builder.HasOne(recipient => recipient.Message)
            .WithMany(message => message.Recipients)
            .HasForeignKey(recipient => recipient.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Пользователь удаляется мягко (IsDeleted), поэтому каскада от него нет.
        builder.HasOne(recipient => recipient.User)
            .WithMany()
            .HasForeignKey(recipient => recipient.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
