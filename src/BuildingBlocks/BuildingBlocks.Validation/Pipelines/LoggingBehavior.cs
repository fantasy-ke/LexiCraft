using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Validation.Pipelines;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        const string prefix = nameof(LoggingBehavior<,>);

        logger.LogInformation(
            "[{Prefix}] Handle request '{RequestData}' and response '{ResponseData}'",
            prefix,
            typeof(TRequest).Name,
            typeof(TResponse).Name
        );

        var timer = new Stopwatch();
        timer.Start();

        var response = await next(cancellationToken);

        timer.Stop();
        var timeTaken = timer.Elapsed;

        if (timeTaken.Seconds > 3)
        {
            logger.LogWarning(
                "[{PerfPossible}] The request '{RequestData}' took '{TimeTaken}' seconds",
                prefix,
                typeof(TRequest).Name,
                timeTaken.Seconds
            );
        }
        else
        {
            logger.LogInformation(
                "[{PerfPossible}] The request '{RequestData}' took '{TimeTaken}' seconds",
                prefix,
                typeof(TRequest).Name,
                timeTaken.Seconds
            );
        }

        logger.LogInformation("[{Prefix}] Handled '{RequestData}'", prefix, typeof(TRequest).Name);

        return response;
    }
}