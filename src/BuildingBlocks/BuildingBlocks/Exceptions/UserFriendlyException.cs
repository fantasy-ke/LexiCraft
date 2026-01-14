using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Exceptions;

/// <summary>
/// 用户友好异常，通常用于向前端展示明确的错误信息
/// </summary>
public class UserFriendlyException(
    string message,
    Exception? innerException = null,
    int statusCode = StatusCodes.Status400BadRequest)
    : CustomException(message, statusCode, innerException);

public static class ThrowUserFriendlyException
{
    /// <summary>
    /// 抛出用户友好异常
    /// </summary>
    [DoesNotReturn]
    public static UserFriendlyException ThrowException(string message, int statusCode = StatusCodes.Status400BadRequest)
    {
        throw new UserFriendlyException(message, null, statusCode);
    }

    /// <summary>
    /// 包装现有异常并抛出用户友好异常
    /// </summary>
    [DoesNotReturn]
    public static UserFriendlyException ThrowException(string message, Exception innerException, int statusCode = StatusCodes.Status400BadRequest)
    {
        throw new UserFriendlyException(message, innerException, statusCode);
    }

    /// <summary>
    /// Exception 扩展方法：快速抛出用户友好异常
    /// </summary>
    [DoesNotReturn]
    public static UserFriendlyException ThrowUserFriendly(this Exception ex, string message, int statusCode = StatusCodes.Status400BadRequest)
    {
        throw new UserFriendlyException(message, ex, statusCode);
    }
}