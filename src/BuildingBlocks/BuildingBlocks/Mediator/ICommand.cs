using MediatR;

namespace BuildingBlocks.Mediator;

/// <summary>
///     命令接口，用于修改数据或触发操作
/// </summary>
/// <typeparam name="TResponse">命令执行后的返回类型</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
///     命令接口，用于修改数据或触发操作（无返回值）
/// </summary>
public interface ICommand : IRequest
{
}