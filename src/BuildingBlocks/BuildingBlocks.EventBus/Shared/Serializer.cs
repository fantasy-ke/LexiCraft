using System.Text.Encodings.Web;
using System.Text.Json;
using BuildingBlocks.Extensions.System;

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
        return data.ToJson(Options);
    }

    public T? Deserialize<T>(string data)
    {
        return data.FromJson<T>(Options);
    }

    public object? Deserialize(string data, Type type)
    {
        return data.FromJson(type, Options);
    }
}