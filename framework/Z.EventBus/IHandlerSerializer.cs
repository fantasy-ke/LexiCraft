namespace Z.EventBus;

/// <summary>
/// Represents the interface for the handler serializer.
/// </summary>
public interface IHandlerSerializer
{
    byte[] Serialize<TEvent>(TEvent eventEvent) where TEvent : class;

    TEvent? Deserialize<TEvent>(ReadOnlyMemory<byte> data) where TEvent : class;

    object? Deserialize(byte[] data, Type type);

    byte[] Serialize(object @event);
    
    TEvent? Deserialize<TEvent>(string data) where TEvent : class;

    object? Deserialize(string data, Type type);

    string SerializeJson(object @event);
}