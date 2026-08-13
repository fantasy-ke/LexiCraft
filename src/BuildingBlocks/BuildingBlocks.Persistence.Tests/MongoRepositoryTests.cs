using BuildingBlocks.Domain.Internal;
using BuildingBlocks.MongoDB;
using MongoDB.Driver;

namespace BuildingBlocks.Persistence.Tests;

public class MongoRepositoryTests
{
    [Fact]
    public async Task Empty_batch_insert_is_a_no_op_without_contacting_the_server()
    {
        var client = new MongoClient(new MongoClientSettings
        {
            Server = new MongoServerAddress("localhost", 1),
            ServerSelectionTimeout = TimeSpan.FromMilliseconds(10)
        });
        using var context = new MongoDbContext(client.GetDatabase("test_database"), client);
        var repository = new MongoRepository<TestAggregate>(context);

        await repository.InsertAsync(Array.Empty<TestAggregate>());
    }

    private sealed class TestAggregate : MongoEntity, IAggregateRoot
    {
    }
}