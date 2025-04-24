namespace Z.Local.EventBus.Serializer;

/// <summary>
/// Represents the interface for the handler serializer.
/// </summary>
public interface IHandlerSerializer
{
    TEvent? Deserialize<TEvent>(string data) where TEvent : class;

    object? Deserialize(string data, Type type);

    string SerializeJson(object @event);
}