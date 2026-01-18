using BuildingBlocks.Caching.Examples;

namespace BuildingBlocks.Caching.Examples;

/// <summary>
/// 测试运行器
/// </summary>
public class TestRunner
{
    /// <summary>
    /// 程序入口点
    /// </summary>
    public static void Main(string[] args)
    {
        try
        {
            BasicComponentsTest.RunAllTests();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试运行失败: {ex}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}