using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Exceptions;

public class ValidationException(string message, Exception? innerException = null, params string[] errors)
    : CustomException(message, StatusCodes.Status400BadRequest, innerException, errors);