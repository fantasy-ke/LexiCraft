using BuildingBlocks.Exceptions.Problem;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Errors;

/// <summary>把常见 MongoDB 异常映射为 HTTP Problem Details 状态码。</summary>
/// <remarks>
///     连接失败映射为 503、驱动超时映射为 408、写入与命令错误映射为 500；
///     非 MongoDB 异常委托给默认映射器，避免替换注册后丢失通用错误语义。
/// </remarks>
public class MongoDbProblemCodeMapper : IProblemCodeMapper
{
    private readonly IProblemCodeMapper _baseProblemCodeMapper = new DefaultProblemCodeMapper();

    /// <summary>获取异常对应的 HTTP 状态码。</summary>
    /// <param name="exception">待映射异常；允许为 <see langword="null"/>。</param>
    /// <returns>适用于 Problem Details 响应的 HTTP 状态码。</returns>
    public int GetMappedStatusCodes(Exception? exception)
    {
        return exception switch
        {
            MongoConnectionException => StatusCodes.Status503ServiceUnavailable,
            MongoWriteException => StatusCodes.Status500InternalServerError,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            MongoCommandException => StatusCodes.Status500InternalServerError,
            _ => _baseProblemCodeMapper.GetMappedStatusCodes(exception)
        };
    }
}
