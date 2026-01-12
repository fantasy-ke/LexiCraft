using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Exceptions;

public class UserFriendlyException(string message, int statusCode = StatusCodes.Status400BadRequest)
    : CustomException(message, statusCode);

public static class ThrowUserFriendlyException
{
    public static void ThrowException(string message, int statusCode = StatusCodes.Status400BadRequest)
    {
        throw new UserFriendlyException(message, statusCode);
    }
}