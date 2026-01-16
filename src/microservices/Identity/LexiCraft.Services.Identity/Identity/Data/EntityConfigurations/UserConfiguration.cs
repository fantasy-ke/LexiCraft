using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LexiCraft.Services.Identity.Identity.Data.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(32)
            .HasComment("昵称");

        builder.Property(x => x.UserAccount)
            .IsRequired()
            .HasMaxLength(32)
            .HasComment("用户名");

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(64)
            .HasComment("密码哈希值");

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(64)
            .HasComment("邮箱");

        builder.Property(x => x.Avatar)
            .HasMaxLength(256)
            .HasComment("头像");

        builder.Property(p => p.Source).HasConversion(new ValueConverter<SourceEnum, int>(
                v => ((int)v),
                v => (SourceEnum)v))
            .HasComment("注册来源");

        builder.HasIndex(x => x.Username)
            .IsUnique();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.Signature)
            .HasMaxLength(500)
            .HasComment("个性签名");
    }
}