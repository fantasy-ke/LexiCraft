using System.Text.Json;
using System.Text.Json.Serialization;

namespace Z.Local.EventBus.Serializer;

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