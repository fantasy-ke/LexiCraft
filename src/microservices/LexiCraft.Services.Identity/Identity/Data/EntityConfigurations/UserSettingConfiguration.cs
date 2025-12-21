using LexiCraft.Services.Identity.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexiCraft.Services.Identity.Identity.Data.EntityConfigurations;

public class UserSettingConfiguration : IEntityTypeConfiguration<UserSetting>
{
    public void Configure(EntityTypeBuilder<UserSetting> builder)
    {
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
    }
}