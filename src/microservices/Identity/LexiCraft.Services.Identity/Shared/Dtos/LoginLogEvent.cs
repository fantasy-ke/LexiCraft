namespace LexiCraft.Services.Identity.Shared.Dtos;

/// <summary>
/// 登录日志DTO
/// </summary>
/// <param name="UserId">登录用户</param>
/// <param name="Username">登录账号</param>
/// <param name="LoginTime">登录时间</param>
/// <param name="Origin">登录来源</param>
/// <param name="Ip">登录客户端ip</param>
/// <param name="UserAgent">登录设备信息</param>
/// <param name="LoginType">登录类型</param>
/// <param name="IsSuccess">登录是否成功</param>
/// <param name="ErrorMessage">错误信息</param>
public record LoginLogEvent(
    Guid? UserId,
    string? Username,
    DateTime LoginTime,
    string? Origin,
    string? Ip,
    string? UserAgent,
    string? LoginType,
    bool IsSuccess,
    string? ErrorMessage);