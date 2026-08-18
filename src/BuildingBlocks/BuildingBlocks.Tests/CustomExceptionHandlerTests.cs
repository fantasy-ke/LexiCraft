using System.Text.Json;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Exceptions.Handler;
using BuildingBlocks.Exceptions.Problem;
using BuildingBlocks.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace BuildingBlocks.Tests;

public class CustomExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WritesMatchingHttpStatusAndResultDto()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/test";
        httpContext.Response.Body = new MemoryStream();
        var handler = new CustomExceptionHandler(
            NullLogger<CustomExceptionHandler>.Instance,
            new TestWebHostEnvironment(),
            [new DefaultProblemCodeMapper()]);
        var exception = new UserFriendlyException(
            "conflict",
            statusCode: StatusCodes.Status409Conflict);

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ResultDto>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(response);
        Assert.False(response!.Status);
        Assert.Equal(StatusCodes.Status409Conflict, response.StatusCode);
        Assert.Equal("conflict", response.Message);
        Assert.Equal("UserFriendlyException", response.Extensions["title"]?.ToString());
        Assert.Equal("/test", response.Extensions["instance"]?.ToString());
        Assert.True(response.Extensions.ContainsKey("traceId"));
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = nameof(BuildingBlocks.Tests);
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
