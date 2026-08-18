using BuildingBlocks.Resilience;

namespace BuildingBlocks.MongoDB.Resilience;

/// <summary>定义仅供 MongoDB 仓储使用的弹性管线，避免覆盖应用中的通用弹性服务。</summary>
/// <remarks>内置仓储只对事务外读取使用该管线；事务内操作和写入不进行应用层局部重试。</remarks>
public interface IMongoResilienceService : IResilienceService
{
}