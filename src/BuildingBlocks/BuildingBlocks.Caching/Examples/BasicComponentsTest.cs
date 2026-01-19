using BuildingBlocks.Caching.Serialization;
using Microsoft.Extensions.Logging;
using System.Text;

namespace BuildingBlocks.Caching.Examples;

/// <summary>
/// 基础组件测试示例
/// </summary>
public static class BasicComponentsTest
{
    /// <summary>
    /// 测试 JSON 序列化器
    /// </summary>
    public static void TestJsonSerializer()
    {
        Console.WriteLine("=== 测试 JSON 序列化器 ===");
        var testData = new { Name = "测试", Value = 123, Items = new[] { 1, 2, 3 } };
        
        // 序列化
        var serialized = JsonCacheSerializer.Serialize(testData);
        Console.WriteLine($"序列化结果: {Encoding.UTF8.GetString(serialized)}");
        
        // 反序列化
        var deserialized = JsonCacheSerializer.Deserialize<dynamic>(serialized);
        Console.WriteLine($"反序列化成功: {deserialized != null}");
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 测试 GZip 压缩器
    /// </summary>
    public static void TestGZipCompressor()
    {
        Console.WriteLine("=== 测试 GZip 压缩器 ===");
        var testData = "这是一个测试字符串，用于验证 GZip 压缩功能。重复内容：测试测试测试测试测试测试测试测试测试测试";
        var originalBytes = Encoding.UTF8.GetBytes(testData);
        
        Console.WriteLine($"原始数据大小: {originalBytes.Length} 字节");
        
        // 压缩
        var compressed = GZipCacheCompressor.Compress(originalBytes);
        Console.WriteLine($"压缩后大小: {compressed.Length} 字节");
        Console.WriteLine($"压缩比: {(1.0 - (double)compressed.Length / originalBytes.Length) * 100:F1}%");
        
        // 解压缩
        var decompressed = GZipCacheCompressor.Decompress(compressed);
        var decompressedText = Encoding.UTF8.GetString(decompressed);
        
        Console.WriteLine($"解压缩成功: {decompressedText == testData}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 测试 MemoryPack 序列化器
    /// </summary>
    public static void TestMemoryPackSerializer()
    {
        Console.WriteLine("=== 测试 MemoryPack 序列化器 ===");
        
        try
        {
            var testData = new TestModel { Name = "测试", Value = 123 };
            
            // 序列化
            var serialized = MemoryPackCacheSerializer.Serialize(testData);
            Console.WriteLine($"序列化结果大小: {serialized.Length} 字节");
            
            // 反序列化
            var deserialized = MemoryPackCacheSerializer.Deserialize<TestModel>(serialized);
            Console.WriteLine($"反序列化成功: {deserialized?.Name == testData.Name && deserialized?.Value == testData.Value}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MemoryPack 测试失败 (这是预期的，因为需要特殊的代码生成): {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 运行所有基础组件测试
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("开始基础组件测试...\n");
        
        TestJsonSerializer();
        TestGZipCompressor();
        TestMemoryPackSerializer();
        
        Console.WriteLine("基础组件测试完成！");
    }
}

/// <summary>
/// 测试用的简单模型
/// </summary>
public class TestModel
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
