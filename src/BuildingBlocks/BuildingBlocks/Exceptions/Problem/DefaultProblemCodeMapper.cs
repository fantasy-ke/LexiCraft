using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Exceptions.Problem;

public class DefaultProblemCodeMapper : IProblemCodeMapper
{
    public int GetMappedStatusCodes(Exception? exception)
    {
        return exception switch
        {
            UserFriendlyException conflictException => conflictException.StatusCode,
            ValidationException validationException => validationException.StatusCode,
            HttpRequestException httpRequestException => (int?)httpRequestException.StatusCode ??
                                                         StatusCodes.Status500InternalServerError,
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentOutOfRangeException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            OperationCanceledException => StatusCodes.Status499ClientClosedRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}

public interface IProblemCodeMapper
{
    int GetMappedStatusCodes(Exception? exception);
}