using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildingBlocks.Extensions.System;

/// <summary>
///     JSON 序列化扩展方法
///     提供基于 System.Text.Json 的序列化和反序列化扩展方法
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    ///     默认的 JSON 序列化选项
    ///     注意：
    ///     1. 不设置 PropertyNamingPolicy，以支持 JsonPropertyName 特性
    ///     2. 序列化时忽略 null 值字段（DefaultIgnoreCondition = WhenWritingNull）
    ///     3. 支持字符串枚举转换（不区分大小写）
    /// </summary>
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 不转义中文等非 ASCII 字符，输出可读字符串
        Converters = { new CaseInsensitiveEnumConverter() }
    };

    /// <summary>
    ///     将对象序列化为 JSON 字符串
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">要序列化的对象</param>
    /// <param name="options">可选的序列化选项，如果为 null 则使用默认选项</param>
    /// <returns>JSON 字符串</returns>
    public static string ToJson<T>(this T obj, JsonSerializerOptions? options = null)
    {
        if (obj == null)
            return string.Empty;

        if (obj is string str)
            return str;

        return JsonSerializer.Serialize(obj, options ?? DefaultOptions);
    }

    /// <summary>
    ///     将 JSON 字符串反序列化为对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="json">JSON 字符串</param>
    /// <param name="options">可选的反序列化选项，如果为 null 则使用默认选项</param>
    /// <returns>反序列化的对象</returns>
    public static T? FromJson<T>(this string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }

    /// <summary>
    ///     将 JSON 字符串反序列化为对象
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <param name="type">对象类型</param>
    /// <param name="options">可选的反序列化选项，如果为 null 则使用默认选项</param>
    /// <returns>反序列化的对象</returns>
    public static object? FromJson(this string json, Type type, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize(json, type, options ?? DefaultOptions);
    }

    /// <summary>
    ///     将对象序列化为 JSON 字节数组
    /// </summary>
    public static byte[] ToJsonBytes<T>(this T obj, JsonSerializerOptions? options = null)
    {
        if (obj == null)
            return Array.Empty<byte>();

        return JsonSerializer.SerializeToUtf8Bytes(obj, options ?? DefaultOptions);
    }

    /// <summary>
    ///     将 JSON 字节数组反序列化为对象
    /// </summary>
    public static T? FromJson<T>(this byte[]? bytes, JsonSerializerOptions? options = null)
    {
        if (bytes == null || bytes.Length == 0)
            return default;

        return JsonSerializer.Deserialize<T>(bytes, options ?? DefaultOptions);
    }
}

/// <summary>
///     不区分大小写的枚举转换器
///     支持将字符串（如 "USD"）转换为枚举值，不区分大小写
///     同时支持数字格式的枚举值
/// </summary>
internal sealed class CaseInsensitiveEnumConverter : JsonConverterFactory
{
    /// <summary>
    ///     判断是否可以转换指定类型
    /// </summary>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    /// <summary>
    ///     创建转换器实例
    /// </summary>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(CaseInsensitiveEnumConverterInner<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    /// <summary>
    ///     内部转换器实现
    /// </summary>
    private sealed class CaseInsensitiveEnumConverterInner<T> : JsonConverter<T> where T : struct, Enum
    {
        /// <summary>
        ///     读取枚举值（支持字符串和数字，不区分大小写）
        /// </summary>
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue)) return default;

                // 尝试不区分大小写匹配枚举名称
                if (Enum.TryParse<T>(stringValue, true, out var enumValue)) return enumValue;

                // 如果匹配失败，返回默认值
                return default;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                var numericValue = reader.GetInt32();
                if (Enum.IsDefined(typeToConvert, numericValue)) return (T)Enum.ToObject(typeToConvert, numericValue);
            }

            return default;
        }

        /// <summary>
        ///     写入枚举值（序列化为字符串）
        /// </summary>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}