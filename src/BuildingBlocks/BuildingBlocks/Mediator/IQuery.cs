namespace BuildingBlocks.Mediator;

/// <summary>
/// 查询接口，用于查询数据
/// </summary>
/// <typeparam name="TResponse">查询结果的返回类型</typeparam>
public interface IQuery<out TResponse> : MediatR.IRequest<TResponse>
{
}