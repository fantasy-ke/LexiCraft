namespace LexiCraft.Infrastructure.Exceptions;

public class UserFriednlyException : Exception
{
    public UserFriednlyException()
    {
    }

    public UserFriednlyException(string message) : base(message)
    {
    }
}

public static class ThrowUserFriendlyException
{
    public static void ThrowException(string message)
    {
        throw new UserFriednlyException(message);
    }
}