using System.Threading.Tasks;

namespace Z.EventBus;

/// <summary>
/// 请求处理者
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventHandler<in TEvent> where TEvent : class
{
    Task HandleAsync(TEvent @event);
}   