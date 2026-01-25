using MediatR;

namespace BuildingBlocks.Mediator;

/// <summary>
///     查询处理器接口
/// </summary>
/// <typeparam name="TQuery">查询类型</typeparam>
/// <typeparam name="TResponse">查询结果的返回类型</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}