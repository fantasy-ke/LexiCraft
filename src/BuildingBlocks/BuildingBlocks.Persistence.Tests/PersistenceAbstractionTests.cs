using BuildingBlocks.Persistence.Abstractions.Repositories;

namespace BuildingBlocks.Persistence.Tests;

public class PersistenceAbstractionTests
{
    [Fact]
    public void Query_repository_contract_has_no_cross_entity_generic_operations()
    {
        var genericMethods = typeof(IQueryRepository<>).GetMethods()
            .Where(method => method.IsGenericMethodDefinition)
            .ToArray();

        Assert.Empty(genericMethods);
    }
}
