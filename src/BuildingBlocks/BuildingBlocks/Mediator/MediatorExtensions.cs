using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Mediator;

public static class MediatorExtensions
{
    /// <summary>
    /// 添加Mediator服务和支持类型
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMediator<T>(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(T));
        });

        return services;
    }
}