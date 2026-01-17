using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Identity.Identity.Models;

public class UserSetting : SimpleAuditEntity<Guid>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// 生日
    /// </summary>
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// 个人简介
    /// </summary>
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    /// 个人资料是否公开
    /// </summary>
    public bool IsProfilePublic { get; set; }

    /// <summary>
    /// 是否展示学习进度
    /// </summary>
    public bool ShowLearningProgress { get; set; }

    /// <summary>
    /// 是否允许接收私信
    /// </summary>
    public bool AllowMessages { get; set; }

    /// <summary>
    /// 是否接收通知
    /// </summary>
    public bool ReceiveNotifications { get; set; }

    /// <summary>
    /// 是否接收邮件更新
    /// </summary>
    public bool ReceiveEmailUpdates { get; set; }

    /// <summary>
    /// 是否接收推送通知
    /// </summary>
    public bool ReceivePushNotifications { get; set; }

    /// <summary>
    /// 账户是否处于激活状态
    /// </summary>
    public bool AccountActive { get; set; }
}
