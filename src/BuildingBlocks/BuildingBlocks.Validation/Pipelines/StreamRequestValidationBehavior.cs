using System.Runtime.CompilerServices;
using System.Text.Json;
using BuildingBlocks.Validation.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Validation.Pipelines;

public class StreamRequestValidationBehavior<TRequest, TResponse>(
    IServiceProvider serviceProvider,
    ILogger<StreamRequestValidationBehavior<TRequest, TResponse>> logger
) : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
    where TResponse : class
{
    private readonly ILogger<StreamRequestValidationBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public async IAsyncEnumerable<TResponse> Handle(TRequest message, 
        StreamHandlerDelegate<TResponse> next, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var validator = _serviceProvider.GetService<IValidator<TRequest>>()!;

        if (validator is null)
        {
            await foreach (var response in next().WithCancellation(cancellationToken))
            {
                yield return response;
            }

            yield break;
        }

        _logger.LogInformation(
            "[{Prefix}] Handle request={RequestData} and response={ResponseData}",
            nameof(StreamRequestValidationBehavior<TRequest, TResponse>),
            typeof(TRequest).Name,
            typeof(TResponse).Name
        );

        _logger.LogDebug(
            "Handling {FullName} with content {Request}",
            typeof(TRequest).FullName,
            JsonSerializer.Serialize(message)
        );

        await validator.HandleValidationAsync(message, cancellationToken: cancellationToken);

        await foreach (var response in next().WithCancellation(cancellationToken))
        {
            yield return response;
            _logger.LogInformation("Handled {FullName}", typeof(TRequest).FullName);
        }
    }
}