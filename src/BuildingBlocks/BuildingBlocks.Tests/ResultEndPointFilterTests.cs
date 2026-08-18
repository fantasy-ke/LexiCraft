using BuildingBlocks.Filters;
using BuildingBlocks.Model;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Tests;

public class ResultEndPointFilterTests
{
    private readonly ResultEndPointFilter _filter = new();

    [Fact]
    public async Task InvokeAsync_WrapsPlainResult()
    {
        var payload = new { Value = "ok" };

        var result = await _filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(new DefaultHttpContext()),
            _ => ValueTask.FromResult<object?>(payload));

        var response = Assert.IsType<ResultDto>(result);
        Assert.True(response.Status);
        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Same(payload, response.Data);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotWrapExistingResultDto()
    {
        var expected = ResultDto.Fail("failed", StatusCodes.Status400BadRequest);

        var result = await _filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(new DefaultHttpContext()),
            _ => ValueTask.FromResult<object?>(expected));

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotWrapIResult()
    {
        var expected = Results.File([], "application/octet-stream");

        var result = await _filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(new DefaultHttpContext()),
            _ => ValueTask.FromResult<object?>(expected));

        Assert.Same(expected, result);
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index)
        {
            return (T)Arguments[index]!;
        }
    }
}
