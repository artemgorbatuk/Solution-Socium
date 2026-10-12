using Datasource.Security.Ef.Common;
using Datasource.Security.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datasource.Security.Ef.Configurations;

public class ConfigurationAppRoleAssignment : IEntityTypeConfiguration<AppRoleAssignment>
{
    public void Configure(EntityTypeBuilder<AppRoleAssignment> builder)
    {
        builder.ToTable("AppRoleAssignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<GuidV7ValueGenerator>();

        builder.Property(assignment => assignment.UserId)
            .IsRequired();

        builder.Property(assignment => assignment.RoleName)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(assignment => new { assignment.UserId, assignment.RoleName }, "UX_AppRoleAssignments_UserId_RoleName")
            .IsUnique();

        builder.HasIndex(assignment => assignment.UserId, "IX_AppRoleAssignments_UserId")
            .IncludeProperties(assignment => assignment.RoleName);

        builder.HasOne(assignment => assignment.User)
            .WithMany()
            .HasForeignKey(assignment => assignment.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
