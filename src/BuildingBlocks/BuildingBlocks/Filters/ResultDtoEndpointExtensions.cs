using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BuildingBlocks.Filters;

/// <summary>
///     控制 Minimal API 端点是否使用 <see cref="Model.ResultDto"/> 响应包络。
/// </summary>
public static class ResultDtoEndpointExtensions
{
    /// <summary>
    ///     为路由组及其子端点启用 <see cref="Model.ResultDto"/> 成功响应包装。
    /// </summary>
    public static RouteGroupBuilder WithResultDto(this RouteGroupBuilder builder)
    {
        builder.WithMetadata(ResultDtoEndpointMetadata.EnabledInstance);
        builder.AddEndpointFilter<ResultEndPointFilter>();
        return builder;
    }

    /// <summary>
    ///     为单个端点启用 <see cref="Model.ResultDto"/> 成功响应包装。
    /// </summary>
    public static RouteHandlerBuilder WithResultDto(this RouteHandlerBuilder builder)
    {
        builder.WithMetadata(ResultDtoEndpointMetadata.EnabledInstance);
        builder.AddEndpointFilter<ResultEndPointFilter>();
        return builder;
    }

    /// <summary>
    ///     关闭继承自上级路由组的 <see cref="Model.ResultDto"/> 成功响应包装。
    /// </summary>
    public static TBuilder WithoutResultDto<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(ResultDtoEndpointMetadata.DisabledInstance);
        return builder;
    }
}

internal sealed record ResultDtoEndpointMetadata(bool Enabled)
{
    public static ResultDtoEndpointMetadata EnabledInstance { get; } = new(true);

    public static ResultDtoEndpointMetadata DisabledInstance { get; } = new(false);
}
