using BuildingBlocks.Domain.Internal;
using BuildingBlocks.EntityFrameworkCore.Converters;

namespace BuildingBlocks.Persistence.Tests;

public class StrongIdValueConverterTests
{
    [Fact]
    public void Converter_rehydrates_strong_id_from_provider_value()
    {
        var converter = new StrongIdValueConverter<TestStrongId, Guid>();
        var value = Guid.NewGuid();

        var result = converter.ConvertFromProviderExpression.Compile()(value);

        Assert.Equal(value, result.Value);
    }

    private sealed record TestStrongId(Guid Value) : StrongId<Guid>(Value);
}
