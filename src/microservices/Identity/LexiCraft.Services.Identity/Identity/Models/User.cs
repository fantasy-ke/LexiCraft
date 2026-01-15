using System.Security.Cryptography;
using BCrypt.Net;
using BuildingBlocks.Domain.Internal;
using LexiCraft.Services.Identity.Identity.Models.Enum;

namespace LexiCraft.Services.Identity.Identity.Models;

public class User: AuditEntity<Guid,Guid?>
{
    public string Avatar { get; set; }

    /// <summary>
    /// 用户账号
    /// </summary>
    public string UserAccount { get; private set; }

    /// <summary>
    /// 用户的用户名，必须唯一。
    /// </summary>
    public string Username { get; private set; }

    /// <summary>
    /// 用户的电子邮件地址，必须唯一。
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// 个人签名
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// 用户的密码哈希值。
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// 用户密码的盐值。
    /// 注意：使用BCrypt时，盐值由算法自动处理，不需要单独存储
    /// </summary>
    [Obsolete("不再需要单独的盐值，BCrypt已内置盐值处理")]
    public string? PasswordSalt { get; set; }

    /// <summary>
    ///  用户来源
    /// </summary>
    public SourceEnum Source { get; private set; }

    /// <summary>
    /// 用户的最后登录日期。
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// 用户的角色列表。
    /// </summary>
    public List<string> Roles { get; private set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 用户设置
    /// </summary>
    public UserSetting? Settings { get; set; }

    /// <summary>
    /// 用户构造函数，初始化用户的基本信息。
    /// </summary>
    /// <param name="userAccount">用户名</param>
    /// <param name="email">电子邮件地址</param>
    public User(string userAccount, string email)
    {
        Id = Guid.NewGuid();
        Username = userAccount;
        UserAccount = userAccount;
        Email = email;
        Roles = [];
    }

    /// <summary>
    /// 设置用户的密码，使用BCrypt进行哈希。
    /// </summary>
    /// <param name="password">用户的明文密码</param>
    public void SetPassword(string password)
    {
        PasswordHash = HashPassword(password);
    }

    /// <summary>
    /// 验证用户输入的密码是否正确。
    /// </summary>
    /// <param name="password">用户输入的明文密码</param>
    /// <returns>如果密码正确则返回true，否则返回false</returns>
    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(PasswordHash))
            return false;

        return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
    }

    /// <summary>
    /// 验证并重新哈希密码（用于升级旧密码哈希或调整工作因子）
    /// </summary>
    /// <param name="password">用户输入的明文密码</param>
    /// <returns>如果需要更新哈希则返回新的哈希值，否则返回null</returns>
    public string? VerifyAndRehashPassword(string password)
    {
        if (string.IsNullOrEmpty(PasswordHash))
            return null;

        // 验证密码并检查是否需要重新哈希（例如，如果工作因子已更改）
        if (BCrypt.Net.BCrypt.Verify(password, PasswordHash))
        {
            // 检查是否需要重新哈希（例如，如果使用了较弱的工作因子）
            if (BCrypt.Net.BCrypt.PasswordNeedsRehash(PasswordHash, 12))
            {
                return HashPassword(password);
            }
        }

        return null;
    }

    /// <summary>
    /// 哈希密码的方法，使用BCrypt算法。
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>哈希后的密码</returns>
    private string HashPassword(string password)
    {
        // 使用BCrypt进行密码哈希，自动处理盐值
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    /// <summary>
    /// 更新用户的最后登录时间。
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 来源
    /// </summary>
    public void UpdateSource(SourceEnum source)
    {
        Source = source;
    }
}