using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Exceptions.Problem;

public class ProblemCodeMapper : IProblemCodeMapper
{
    public int GetMappedStatusCodes(Exception? exception)
    {
        return exception switch
        {
            UserFriendlyException conflictException => conflictException.StatusCode,
            ValidationException validationException => validationException.StatusCode,
            HttpRequestException httpRequestException => (int?)httpRequestException.StatusCode ??
                                                         StatusCodes.Status500InternalServerError,
            ArgumentOutOfRangeException => StatusCodes.Status400BadRequest,
            OperationCanceledException => StatusCodes.Status499ClientClosedRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}

public interface IProblemCodeMapper
{
    int GetMappedStatusCodes(Exception? exception);
}