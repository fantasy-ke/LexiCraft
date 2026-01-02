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
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string SerializeJson<T>(T data) => JsonSerializer.Serialize(data, Options);
    public T? Deserialize<T>(string data) => JsonSerializer.Deserialize<T>(data, Options);
    public object? Deserialize(string data, Type type) => JsonSerializer.Deserialize(data, type, Options);
}
