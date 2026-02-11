using BuildingBlocks.Extensions.System;

namespace BuildingBlocks.OSS.Utils;

internal static class JsonUtil
{
    /// <summary>
    ///     将对象序列化为JSON格式
    /// </summary>
    /// <param name="o">对象</param>
    /// <returns>json字符串</returns>
    public static string SerializeObject(object o)
    {
        return o.ToJson();
    }

    /// <summary>
    ///     解析JSON字符串生成对象实体
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="json">json字符串(eg.{"ID":"112","Name":"石子儿"})</param>
    /// <returns>对象实体</returns>
    public static T? DeserializeObject<T>(string json) where T : class
    {
        return json.FromJson<T>();
    }
}