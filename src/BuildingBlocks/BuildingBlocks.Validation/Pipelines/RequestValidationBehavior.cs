using System.Text.Json;
using BuildingBlocks.Validation.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Validation.Pipelines;

public class RequestValidationBehavior<TRequest, TResponse>(
    IServiceProvider serviceProvider,
    ILogger<RequestValidationBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    public async Task<TResponse> Handle(TRequest message,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validator = serviceProvider.GetService<IValidator<TRequest>>()!;
        if (validator is null)
            return await next(cancellationToken);

        logger.LogInformation(
            "[{Prefix}] Handle request={RequestData} and response={ResponseData}",
            nameof(RequestValidationBehavior<,>),
            typeof(TRequest).Name,
            typeof(TResponse).Name
        );

        logger.LogDebug(
            "Handling {FullName} with content {Request}",
            typeof(TRequest).FullName,
            JsonSerializer.Serialize(message)
        );

        await validator.HandleValidationAsync(message, cancellationToken);

        var response = await next(cancellationToken);

        logger.LogInformation("Handled {FullName}", typeof(TRequest).FullName);
        return response;
    }
}