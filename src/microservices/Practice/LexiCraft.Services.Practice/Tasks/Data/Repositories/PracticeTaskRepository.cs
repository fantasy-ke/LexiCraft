using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.MongoDB.Repositories;
using BuildingBlocks.MongoDB.Resilience;
using LexiCraft.Services.Practice.Shared.Data;
using LexiCraft.Services.Practice.Tasks.Models;

namespace LexiCraft.Services.Practice.Tasks.Data.Repositories;

/// <summary>
///     Binds the PracticeTask aggregate to its stable MongoDB collection name.
/// </summary>
public sealed class PracticeTaskRepository(
    IMongoDbContext context,
    IMongoResilienceService resilienceService,
    IMongoPerformanceMonitor performanceMonitor)
    : MongoRepository<PracticeTask>(
        context,
        resilienceService,
        performanceMonitor,
        PracticeDbContext.PracticeTasksCollectionName);
