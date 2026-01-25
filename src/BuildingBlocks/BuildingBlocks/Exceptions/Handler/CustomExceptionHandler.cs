using System.Diagnostics;
using BuildingBlocks.Exceptions.Problem;
using BuildingBlocks.Model;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Exceptions.Handler;

public class CustomExceptionHandler(
    ILogger<CustomExceptionHandler> logger,
    IWebHostEnvironment webHostEnvironment,
    IEnumerable<IProblemCodeMapper> problemCodeMappers)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            "Error Message: {exceptionMessage}, Time of occurrence {time}",
            exception.Message, DateTime.UtcNow);

        var statusCode = !problemCodeMappers.Any()
            ? new DefaultProblemCodeMapper().GetMappedStatusCodes(exception)
            : problemCodeMappers.Select(m => m.GetMappedStatusCodes(exception)).FirstOrDefault();
        var extensions = new Dictionary<string, object?>
        {
            { "traceId", Activity.Current?.Id ?? context.TraceIdentifier },
            { "title", exception.GetType().Name },
            { "instance", context.Request.Path }
        };

        if (webHostEnvironment.IsDevelopment()) extensions["stackTrace"] = exception.StackTrace;
        var response = ResultDto.FailExt(exception.Message, extensions, statusCode);
        await context.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}