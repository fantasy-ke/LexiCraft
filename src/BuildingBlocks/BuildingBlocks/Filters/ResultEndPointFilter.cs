using BuildingBlocks.Model;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Filters;

public sealed class ResultEndPointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ResultDtoEndpointMetadata>();
        if (metadata is { Enabled: false })
            return result;

        return result is IResult or ResponseBase ? result : ResultDto.Success(result);
    }
}