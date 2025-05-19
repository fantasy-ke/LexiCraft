namespace BuildingBlocks.Exceptions;

public class UserFriendlyException : Exception
{
    public UserFriendlyException()
    {
    }

    public UserFriendlyException(string message) : base(message)
    {
    }
}

public static class ThrowUserFriendlyException
{
    public static void ThrowException(string message)
    {
        throw new UserFriendlyException(message);
    }
}