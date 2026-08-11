using System.Collections.Concurrent;
using System.Linq.Expressions;
using BuildingBlocks.EventBus.Abstractions;

namespace BuildingBlocks.EventBus.Shared;

public sealed class EventHandlerInvoker
{
    private readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> _handlerCache = new();

    public Task InvokeAsync(object handler, Type eventType, object eventData, CancellationToken cancellationToken)
    {
        var invoker = _handlerCache.GetOrAdd(eventType, CreateInvoker);
        return invoker(handler, eventData, cancellationToken);
    }

    private static Func<object, object, CancellationToken, Task> CreateInvoker(Type eventType)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handleMethod = handlerType.GetMethod(nameof(IEventHandler<object>.HandleAsync),
            [eventType, typeof(CancellationToken)])
            ?? throw new InvalidOperationException($"无法找到事件处理方法: {handlerType.FullName}");

        var handlerParameter = Expression.Parameter(typeof(object), "handler");
        var eventParameter = Expression.Parameter(typeof(object), "eventData");
        var tokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var call = Expression.Call(
            Expression.Convert(handlerParameter, handlerType),
            handleMethod,
            Expression.Convert(eventParameter, eventType),
            tokenParameter);

        return Expression
            .Lambda<Func<object, object, CancellationToken, Task>>(
                call,
                handlerParameter,
                eventParameter,
                tokenParameter)
            .Compile();
    }
}
