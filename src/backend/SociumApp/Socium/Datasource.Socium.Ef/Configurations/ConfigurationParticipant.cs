using Datasource.Socium.Ef.Common;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Socium.Ef.Configurations;

public class ConfigurationParticipant : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("Participants");

        builder.HasKey(participant => participant.Id);

        builder.Property(participant => participant.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(participant => participant.ChatId)
            .IsRequired();

        builder.Property(participant => participant.UserId)
            .IsRequired();

        builder.HasIndex(participant => new { participant.ChatId, participant.UserId }, "IX_Participants_ChatId_UserId")
            .IsUnique();

        builder.HasIndex(participant => participant.UserId, "IX_Participants_UserId");

        builder.HasOne(participant => participant.Chat)
            .WithMany(chat => chat.Participants)
            .HasForeignKey(participant => participant.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
