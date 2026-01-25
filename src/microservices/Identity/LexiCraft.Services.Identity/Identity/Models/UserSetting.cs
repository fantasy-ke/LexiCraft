using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Identity.Identity.Models;

public class UserSetting : SimpleAuditEntity<Guid>
{
    private UserSetting()
    {
    } // For EF Core

    public UserSetting(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        // Default settings
        IsProfilePublic = true;
        ShowLearningProgress = true;
        AllowMessages = true;
        ReceiveNotifications = true;
        ReceiveEmailUpdates = true;
        ReceivePushNotifications = true;
        AccountActive = true;
    }

    /// <summary>
    ///     用户ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    ///     性别
    /// </summary>
    public string Gender { get; private set; } = string.Empty;

    /// <summary>
    ///     生日
    /// </summary>
    public DateTime? Birthday { get; private set; }

    /// <summary>
    ///     个人简介
    /// </summary>
    public string Bio { get; private set; } = string.Empty;

    /// <summary>
    ///     个人资料是否公开
    /// </summary>
    public bool IsProfilePublic { get; private set; }

    /// <summary>
    ///     是否展示学习进度
    /// </summary>
    public bool ShowLearningProgress { get; private set; }

    /// <summary>
    ///     是否允许接收私信
    /// </summary>
    public bool AllowMessages { get; private set; }

    /// <summary>
    ///     是否接收通知
    /// </summary>
    public bool ReceiveNotifications { get; private set; }

    /// <summary>
    ///     是否接收邮件更新
    /// </summary>
    public bool ReceiveEmailUpdates { get; private set; }

    /// <summary>
    ///     是否接收推送通知
    /// </summary>
    public bool ReceivePushNotifications { get; private set; }

    /// <summary>
    ///     账户是否处于激活状态
    /// </summary>
    public bool AccountActive { get; private set; }

    public void UpdateProfile(string gender, DateTime? birthday, string bio)
    {
        Gender = gender;
        Birthday = birthday;
        Bio = bio;
    }

    public void UpdatePrivacy(bool isProfilePublic, bool showLearningProgress, bool allowMessages)
    {
        IsProfilePublic = isProfilePublic;
        ShowLearningProgress = showLearningProgress;
        AllowMessages = allowMessages;
    }

    public void UpdateNotificationSettings(bool receiveNotifications, bool receiveEmailUpdates,
        bool receivePushNotifications)
    {
        ReceiveNotifications = receiveNotifications;
        ReceiveEmailUpdates = receiveEmailUpdates;
        ReceivePushNotifications = receivePushNotifications;
    }

    public void SetAccountStatus(bool isActive)
    {
        AccountActive = isActive;
    }
}