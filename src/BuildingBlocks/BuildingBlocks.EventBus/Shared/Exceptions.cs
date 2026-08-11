namespace BuildingBlocks.EventBus.Shared;

public class EventBusException(string message, Exception? innerException = null) : Exception(message, innerException);

public class EventClientException(string message, Exception? innerException = null)
    : EventBusException(message, innerException);

public class ChannelNullException(string message, Exception? innerException = null)
    : EventBusException(message, innerException);
