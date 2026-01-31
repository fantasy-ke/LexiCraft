using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Identity.Identity.Models;

public class LoginLog : Entity<long>
{
    private LoginLog()
    {
    } // For EF Core

    public LoginLog(string loginType, DateTime loginTime, string? ip, string? userAgent, string? origin)
    {
        LoginType = loginType;
        LoginTime = loginTime;
        Ip = ip;
        UserAgent = userAgent;
        Origin = origin;
    }

    /// <summary>
    ///     用户ID
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    ///     用户名
    /// </summary>
    public string? Username { get; private set; }

    /// <summary>
    ///     登录令牌
    /// </summary>
    public string? Token { get; private set; }

    /// <summary>
    ///     登录时间
    /// </summary>
    public DateTime LoginTime { get; private set; }

    /// <summary>
    ///     登录来源
    /// </summary>
    public string? Origin { get; private set; }

    /// <summary>
    ///     登录客户端IP
    /// </summary>
    public string? Ip { get; private set; }

    /// <summary>
    ///     登录设备信息
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    ///     登录类型
    /// </summary>
    public string LoginType { get; private set; } = string.Empty;

    /// <summary>
    ///     登录是否成功
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    ///     登录/操作信息
    /// </summary>
    public string? Message { get; private set; }

    public void SetUser(Guid? userId, string? username)
    {
        UserId = userId;
        Username = username;
    }

    public void SetSuccess(string? token, string? message)
    {
        IsSuccess = true;
        Token = token;
        Message = message;
    }

    public void SetFailure(string message)
    {
        IsSuccess = false;
        Message = message;
    }
}