using LexiCraft.Domain.Users;
using LexiCraft.Domain.Users.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LexiCraft.Infrastructure.EntityFrameworkCore.Extensions;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ConfigureAuth(this ModelBuilder model)
    {
        model.Entity<User>(builder =>
        {
            builder.ToTable("users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(32)
                .HasComment("用户名");

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(64)
                .HasComment("密码哈希值");

            builder.Property(x => x.PasswordSalt)
                .IsRequired();

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
        });

        model.Entity<UserOAuth>(builder =>
        {
            builder.ToTable("user-oauth");

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
        });

        model.Entity<UserSetting>(builder =>
        {
            builder.ToTable("user-settings");
            builder.HasKey(p => p.Id);
            
            builder.HasIndex(x => x.UserId);
            
            builder.Property(p => p.IsProfilePublic)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("个人资料是否公开");
            
            builder.Property(p => p.ShowLearningProgress)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("显示学习进度");
            
            builder.Property(p => p.AllowMessages)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("允许消息");
            
            builder.Property(p => p.ReceiveNotifications)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("接收通知");
            
            builder.Property(p => p.ReceiveEmailUpdates)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("接收邮件更新");
            
            builder.Property(p => p.ReceivePushNotifications)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("接收推送通知");
            
            builder.Property(p => p.AccountActive)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("账户是否激活");
        });


        return model;
    }
}