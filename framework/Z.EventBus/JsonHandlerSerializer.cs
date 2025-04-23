using System.Text.Json;
using System.Text.Json.Serialization;

namespace Z.EventBus;

public class JsonHandlerSerializer : IHandlerSerializer
{
    private readonly JsonSerializerOptions? _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public byte[] Serialize<TEvent>(TEvent eventEvent) where TEvent : class
    {
        return JsonSerializer.SerializeToUtf8Bytes(eventEvent, _jsonSerializerOptions);
    }

    public TEvent? Deserialize<TEvent>(ReadOnlyMemory<byte> data) where TEvent : class
    {
        return JsonSerializer.Deserialize<TEvent>(data.Span, _jsonSerializerOptions);
    }
    
    public TEvent? Deserialize<TEvent>(byte[] data) where TEvent : class
    {
        return JsonSerializer.Deserialize<TEvent>(data, _jsonSerializerOptions);
    }

    public object? Deserialize(byte[] data, Type type)
    {
        return JsonSerializer.Deserialize(data, type, _jsonSerializerOptions);
    }

    public byte[] Serialize(object @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event);
    }
    
    public TEvent? Deserialize<TEvent>(string data) where TEvent : class
    {
        return JsonSerializer.Deserialize<TEvent>(data, _jsonSerializerOptions);
    }

    public object? Deserialize(string data, Type type)
    {
        return JsonSerializer.Deserialize(data, type, _jsonSerializerOptions);
    }

    public string SerializeJson(object @event)
    {
        return JsonSerializer.Serialize(@event,_jsonSerializerOptions);
    }
}