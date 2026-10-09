using Datasource.Socium.Ef.Common;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Socium.Ef.Configurations;

public class ConfigurationMessage : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(message => message.ChatId)
            .IsRequired();

        // Длина сообщения намеренно не ограничивается.
        builder.Property(message => message.Text)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(message => message.CreatedAt)
            .IsRequired();

        builder.HasIndex(message => new { message.ChatId, message.CreatedAt }, "IX_Messages_ChatId_CreatedAt");

        builder.HasOne(message => message.Chat)
            .WithMany(chat => chat.Messages)
            .HasForeignKey(message => message.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
