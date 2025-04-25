namespace LexiCraft.Infrastructure.Exceptions;

public class AuthLoginException : Exception
{
    public AuthLoginException()
    {
    }

    public AuthLoginException(string message) : base(message)
    {
    }
}

public static class ThrowAuthLoginException
{
    public static void ThrowException(string message)
    {
        throw new AuthLoginException(message);
    }
}