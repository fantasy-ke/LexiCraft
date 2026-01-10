using BuildingBlocks.Exceptions.Problem;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class MongoDbProblemCodeMapper : IProblemCodeMapper
{
    private readonly IProblemCodeMapper _baseProblemCodeMapper = new DefaultProblemCodeMapper();

    public int GetMappedStatusCodes(Exception? exception)
    {
        return exception switch
        {
            MongoConnectionException => StatusCodes.Status503ServiceUnavailable,
            MongoWriteException => StatusCodes.Status500InternalServerError,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            MongoCommandException => StatusCodes.Status500InternalServerError,
            _ => _baseProblemCodeMapper.GetMappedStatusCodes(exception)
        };
    }
}
