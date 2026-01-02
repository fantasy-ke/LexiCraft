namespace BuildingBlocks.EventBus.Shared;

public class EventBusException(string message) : Exception(message);
public class EventClientException(string message) : EventBusException(message);
public class ChannelNullException(string message) : EventBusException(message);
