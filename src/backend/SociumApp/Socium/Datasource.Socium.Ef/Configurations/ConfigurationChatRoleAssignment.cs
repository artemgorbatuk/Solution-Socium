using Datasource.Socium.Ef.Common;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Socium.Ef.Configurations;

public class ConfigurationChatRoleAssignment : IEntityTypeConfiguration<ChatRoleAssignment>
{
    public void Configure(EntityTypeBuilder<ChatRoleAssignment> builder)
    {
        builder.ToTable("ChatRoleAssignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(assignment => assignment.UserId)
            .IsRequired();

        builder.Property(assignment => assignment.ChatId)
            .IsRequired();

        builder.Property(assignment => assignment.RoleName)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(assignment => new { assignment.UserId, assignment.ChatId, assignment.RoleName }, "UX_ChatRoleAssignments_UserId_ChatId_RoleName")
            .IsUnique();

        builder.HasIndex(assignment => new { assignment.UserId, assignment.ChatId }, "IX_ChatRoleAssignments_UserId_ChatId")
            .IncludeProperties(assignment => assignment.RoleName);

        builder.HasOne(assignment => assignment.Chat)
            .WithMany()
            .HasForeignKey(assignment => assignment.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
