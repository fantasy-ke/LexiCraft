using Asp.Versioning;
using BuildingBlocks.Authentication;
using BuildingBlocks.MongoDB.Performance;
using LexiCraft.Shared.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;

namespace LexiCraft.Services.Practice.Shared.Features.GetPerformanceMetrics;

/// <summary>
/// 获取MongoDB性能指标的API端点
/// </summary>
public static class GetPerformanceMetricsEndpoint
{
    public static IEndpointRouteBuilder MapGetPerformanceMetricsEndpoint(this WebApplication app)
    {
        app.MapGet("/api/v{version:apiVersion}/practice/metrics/performance", GetPerformanceMetricsAsync)
            .WithName("GetPerformanceMetrics")
            .WithDisplayName("获取性能指标")
            .WithSummary("获取MongoDB性能指标用于监控")
            .WithDescription("""
                返回练习服务MongoDB操作的综合性能指标。
                
                **指标包括:**
                - 响应时间统计 (平均值、最小值、最大值)
                - 操作计数和速率
                - 慢操作检测和分析
                - 特定集合的性能数据
                - 操作类型分解
                
                **查询参数:**
                - `periodMinutes`: 指标聚合的时间段 (默认: 5分钟, 最大: 60)
                
                **请求示例:**
                ```
                GET /api/v1/practice/metrics/performance?periodMinutes=10
                ```
                
                **响应示例:**
                ```json
                {
                  "periodMinutes": 10,
                  "totalOperations": 1250,
                  "averageResponseTimeMs": 45.2,
                  "maxResponseTimeMs": 320.5,
                  "minResponseTimeMs": 2.1,
                  "operationsPerSecond": 2.08,
                  "slowOperations": 15,
                  "slowOperationPercentage": 1.2,
                  "operationsByCollection": {
                    "practice_tasks": 500,
                    "answer_records": 600,
                    "mistake_items": 150
                  },
                  "operationsByType": {
                    "find": 800,
                    "insert": 300,
                    "update": 150
                  },
                  "timestamp": "2024-01-07T10:00:00Z"
                }
                ```
                """)
            .WithApiVersionSet(app.NewApiVersionSet("Practice").Build())
            .HasApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization(new ZAuthorizeAttribute(PracticePermissions.Performance.Query))
            .WithTags("Performance")
            .Produces<PerformanceMetricsResponse>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized, typeof(ProblemDetails), "application/json")
            .Produces(StatusCodes.Status403Forbidden, typeof(ProblemDetails), "application/json")
            .WithOpenApi(operation =>
            {
                // Add parameter descriptions
                foreach (var parameter in operation.Parameters)
                {
                    if (parameter.Name == "periodMinutes")
                    {
                        parameter.Description = "Time period for metrics aggregation in minutes (default: 5, max: 60)";
                    }
                }
                return operation;
            });

        return app;
    }

    private static async Task<IResult> GetPerformanceMetricsAsync(
        IMongoPerformanceMonitor performanceMonitor,
        [FromQuery] [Range(1, 60)] int? periodMinutes = 5)
    {
        var period = TimeSpan.FromMinutes(periodMinutes ?? 5);
        var metrics = await performanceMonitor.GetMetricsAsync(period);
        
        var response = new PerformanceMetricsResponse
        {
            PeriodMinutes = (int)period.TotalMinutes,
            TotalOperations = metrics.TotalOperations,
            AverageResponseTimeMs = metrics.AverageResponseTime.TotalMilliseconds,
            MaxResponseTimeMs = metrics.MaxResponseTime.TotalMilliseconds,
            MinResponseTimeMs = metrics.MinResponseTime.TotalMilliseconds,
            OperationsPerSecond = metrics.OperationsPerSecond,
            SlowOperations = metrics.SlowOperations,
            SlowOperationPercentage = metrics.TotalOperations > 0 
                ? (double)metrics.SlowOperations / metrics.TotalOperations * 100 
                : 0,
            OperationsByCollection = metrics.OperationsByCollection,
            OperationsByType = metrics.OperationsByType,
            Timestamp = DateTime.UtcNow
        };
        
        return Results.Ok(response);
    }
}

/// <summary>
/// 性能指标响应模型
/// </summary>
public class PerformanceMetricsResponse
{
    /// <summary>
    /// 指标聚合的时间段（分钟）
    /// </summary>
    /// <example>5</example>
    public int PeriodMinutes { get; set; }
    
    /// <summary>
    /// 时间段内数据库操作总数
    /// </summary>
    /// <example>1250</example>
    public int TotalOperations { get; set; }
    
    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    /// <example>45.2</example>
    public double AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// 最大响应时间（毫秒）
    /// </summary>
    /// <example>320.5</example>
    public double MaxResponseTimeMs { get; set; }
    
    /// <summary>
    /// 最小响应时间（毫秒）
    /// </summary>
    /// <example>2.1</example>
    public double MinResponseTimeMs { get; set; }
    
    /// <summary>
    /// 每秒操作数
    /// </summary>
    /// <example>2.08</example>
    public double OperationsPerSecond { get; set; }
    
    /// <summary>
    /// 慢操作数量（>200毫秒）
    /// </summary>
    /// <example>15</example>
    public int SlowOperations { get; set; }
    
    /// <summary>
    /// 慢操作百分比
    /// </summary>
    /// <example>1.2</example>
    public double SlowOperationPercentage { get; set; }
    
    /// <summary>
    /// 按MongoDB集合统计的操作数
    /// </summary>
    public Dictionary<string, int> OperationsByCollection { get; set; } = new();
    
    /// <summary>
    /// 按操作类型统计的操作数（查找、插入、更新、删除）
    /// </summary>
    public Dictionary<string, int> OperationsByType { get; set; } = new();
    
    /// <summary>
    /// 指标生成时间戳
    /// </summary>
    /// <example>2024-01-07T10:00:00Z</example>
    public DateTime Timestamp { get; set; }
}