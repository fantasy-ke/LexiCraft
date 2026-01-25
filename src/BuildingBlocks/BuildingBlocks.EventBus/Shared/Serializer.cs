using System.Text.Encodings.Web;
using System.Text.Json;

namespace BuildingBlocks.EventBus.Shared;

public interface IHandlerSerializer
{
    string SerializeJson<T>(T data);
    T? Deserialize<T>(string data);
    object? Deserialize(string data, Type type);
}

public class JsonHandlerSerializer : IHandlerSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string SerializeJson<T>(T data)
    {
        return JsonSerializer.Serialize(data, Options);
    }

    public T? Deserialize<T>(string data)
    {
        return JsonSerializer.Deserialize<T>(data, Options);
    }

    public object? Deserialize(string data, Type type)
    {
        return JsonSerializer.Deserialize(data, type, Options);
    }
}