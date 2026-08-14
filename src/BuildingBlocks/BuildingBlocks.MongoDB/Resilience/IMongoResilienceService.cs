using BuildingBlocks.Resilience;

namespace BuildingBlocks.MongoDB.Resilience;

/// <summary>
///     MongoDB-specific resilience pipeline used by Mongo repositories.
/// </summary>
public interface IMongoResilienceService : IResilienceService
{
}