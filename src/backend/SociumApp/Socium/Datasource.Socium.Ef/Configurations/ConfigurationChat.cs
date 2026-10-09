using Datasource.Socium.Ef.Common;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Socium.Ef.Configurations;

public class ConfigurationChat : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.ToTable("Chats");

        builder.HasKey(chat => chat.Id);

        builder.Property(chat => chat.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(chat => chat.RoomId)
            .IsRequired();

        builder.Property(chat => chat.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(chat => chat.RoomId, "IX_Chats_RoomId");

        builder.HasOne(chat => chat.Room)
            .WithMany(room => room.Chats)
            .HasForeignKey(chat => chat.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
