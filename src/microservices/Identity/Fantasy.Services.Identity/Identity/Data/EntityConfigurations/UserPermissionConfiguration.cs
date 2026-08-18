using Fantasy.Services.Identity.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Services.Identity.Identity.Data.EntityConfigurations;

public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.Property(permission => permission.PermissionName)
            .HasMaxLength(200);

        builder.HasIndex(permission => new { permission.UserId, permission.PermissionName })
            .IsUnique();
    }
}
