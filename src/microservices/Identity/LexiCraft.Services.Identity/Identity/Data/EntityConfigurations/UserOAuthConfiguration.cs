using LexiCraft.Services.Identity.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexiCraft.Services.Identity.Identity.Data.EntityConfigurations;

public class UserOAuthConfiguration : IEntityTypeConfiguration<UserOAuth>
{
    public void Configure(EntityTypeBuilder<UserOAuth> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasComment("OAuth 提供者");

        builder.Property(x => x.ProviderUserId)
            .HasComment("OAuth 提供者用户 ID");

        builder.HasIndex(x => x.UserId);

        // 聚合索引
        builder.HasIndex(x => new { x.Provider, x.ProviderUserId })
            .IsUnique();

        builder.HasIndex(x => new { x.Provider, x.ProviderUserId, x.UserId });
    }
}