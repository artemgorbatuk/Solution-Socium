using Datasource.Socium.Ef.Common;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Socium.Ef.Configurations;

public class ConfigurationRoomRoleAssignment : IEntityTypeConfiguration<RoomRoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoomRoleAssignment> builder)
    {
        builder.ToTable("RoomRoleAssignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(assignment => assignment.UserId)
            .IsRequired();

        builder.Property(assignment => assignment.RoomId)
            .IsRequired();

        builder.Property(assignment => assignment.RoleName)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(assignment => new { assignment.UserId, assignment.RoomId, assignment.RoleName }, "UX_RoomRoleAssignments_UserId_RoomId_RoleName")
            .IsUnique();

        builder.HasIndex(assignment => new { assignment.UserId, assignment.RoomId }, "IX_RoomRoleAssignments_UserId_RoomId")
            .IncludeProperties(assignment => assignment.RoleName);

        builder.HasOne(assignment => assignment.Room)
            .WithMany()
            .HasForeignKey(assignment => assignment.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
