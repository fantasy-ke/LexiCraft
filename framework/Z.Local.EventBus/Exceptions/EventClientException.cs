namespace Z.Local.EventBus.Exceptions;

public class EventClientException : Exception
{
    public EventClientException()
    {
    }

    public EventClientException(string message) : base(message)
    {
    }
}

public static class ThrowEventClientException
{
    public static void ThrowException(string str)
    {
        throw new EventClientException($"事件处理异常信息{str}");
    }
}