using Datasource.Socium.Ef.Common;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Socium.Ef.Configurations;

public class ConfigurationSender : IEntityTypeConfiguration<Sender>
{
    public void Configure(EntityTypeBuilder<Sender> builder)
    {
        builder.ToTable("Senders");

        builder.HasKey(sender => sender.Id);

        builder.Property(sender => sender.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(sender => sender.MessageId)
            .IsRequired();

        builder.Property(sender => sender.UserId)
            .IsRequired();

        builder.HasIndex(sender => sender.MessageId, "IX_Senders_MessageId")
            .IsUnique();

        builder.HasIndex(sender => sender.UserId, "IX_Senders_UserId");

        builder.HasOne(sender => sender.Message)
            .WithOne(message => message.Sender)
            .HasForeignKey<Sender>(sender => sender.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
