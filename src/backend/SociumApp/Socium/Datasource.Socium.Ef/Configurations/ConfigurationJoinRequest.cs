using Datasource.Socium.Ef.Common;
using Datasource.Socium.Ef.Enums;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Socium.Ef.Configurations;

public class ConfigurationJoinRequest : IEntityTypeConfiguration<JoinRequest>
{
    public void Configure(EntityTypeBuilder<JoinRequest> builder)
    {
        builder.ToTable("JoinRequests");

        builder.HasKey(joinRequest => joinRequest.Id);

        builder.Property(joinRequest => joinRequest.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(joinRequest => joinRequest.ChatId)
            .IsRequired();

        builder.Property(joinRequest => joinRequest.UserId)
            .IsRequired();

        builder.Property(joinRequest => joinRequest.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(joinRequest => joinRequest.CreatedByUserId)
            .IsRequired();

        builder.Property(joinRequest => joinRequest.RespondedByUserId);

        builder.Property(joinRequest => joinRequest.RespondedAt);

        builder.HasIndex(joinRequest => new { joinRequest.ChatId, joinRequest.UserId }, "UX_JoinRequests_ChatId_UserId_Pending")
            .IsUnique()
            .HasFilter($"\"Status\" = {(int)JoinRequestStatus.Pending}");

        builder.HasIndex(joinRequest => new { joinRequest.ChatId, joinRequest.Status }, "IX_JoinRequests_ChatId_Status");

        builder.HasOne(joinRequest => joinRequest.Chat)
            .WithMany(chat => chat.JoinRequests)
            .HasForeignKey(joinRequest => joinRequest.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
