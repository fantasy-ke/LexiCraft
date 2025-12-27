using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Exceptions;

public class UserFriendlyException(string message) : CustomException(message);

public static class ThrowUserFriendlyException
{
    public static void ThrowException(string message)
    {
        throw new UserFriendlyException(message);
    }
}