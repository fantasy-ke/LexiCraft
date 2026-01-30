using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Internal;

namespace BuildingBlocks.Serialization.Json;

/// <summary>
///     强类型 ID 的 JSON 转换器
/// </summary>
/// <typeparam name="TStrongId"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class StrongIdJsonConverter<TStrongId, TValue> : JsonConverter<TStrongId>
    where TStrongId : StrongId<TValue>
    where TValue : notnull, IComparable<TValue>, IComparable
{
    public override TStrongId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = JsonSerializer.Deserialize<TValue>(ref reader, options);
        return value == null ? null : (TStrongId)Activator.CreateInstance(typeof(TStrongId), value)!;
    }

    public override void Write(Utf8JsonWriter writer, TStrongId value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}
