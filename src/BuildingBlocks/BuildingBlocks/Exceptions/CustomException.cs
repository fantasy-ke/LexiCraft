using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Exceptions;

public class CustomException : Exception
{
    protected CustomException(
        string message,
        int statusCode = StatusCodes.Status500InternalServerError,
        Exception? innerException = null,
        params string[] errors
    )
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; protected set; }

    public override string ToString()
    {
        return GetType().FullName ?? GetType().Name;
    }
}