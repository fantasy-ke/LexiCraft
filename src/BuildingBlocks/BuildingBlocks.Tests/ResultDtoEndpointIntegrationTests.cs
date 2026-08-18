using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace BuildingBlocks.Tests;

public class ResultDtoEndpointIntegrationTests
{
    [Fact]
    public async Task RouteGroup_CanWrapAndOptOutPerEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        var api = app.MapGroup("/api").WithResultDto();
        api.MapGet("/wrapped", () => new SampleResponse("wrapped"));
        api.MapGet("/raw", () => new SampleResponse("raw")).WithoutResultDto();
        api.MapGet("/result", () => Results.Ok(new SampleResponse("result")));
        var internalApi = api.MapGroup("/internal").WithoutResultDto();
        internalApi.MapGet("/raw", () => new SampleResponse("group-raw"));
        internalApi.MapGet("/wrapped", () => new SampleResponse("group-wrapped")).WithResultDto();
        app.MapGet("/single", () => new SampleResponse("single")).WithResultDto();
        await app.StartAsync();
        using var client = app.GetTestClient();

        var wrapped = await client.GetFromJsonAsync<JsonElement>("/api/wrapped");
        var raw = await client.GetFromJsonAsync<JsonElement>("/api/raw");
        var result = await client.GetFromJsonAsync<JsonElement>("/api/result");
        var groupRaw = await client.GetFromJsonAsync<JsonElement>("/api/internal/raw");
        var groupWrapped = await client.GetFromJsonAsync<JsonElement>("/api/internal/wrapped");
        var single = await client.GetFromJsonAsync<JsonElement>("/single");

        Assert.True(wrapped.GetProperty("status").GetBoolean());
        Assert.Equal("wrapped", wrapped.GetProperty("data").GetProperty("value").GetString());
        Assert.Equal("raw", raw.GetProperty("value").GetString());
        Assert.False(raw.TryGetProperty("status", out _));
        Assert.Equal("result", result.GetProperty("value").GetString());
        Assert.False(result.TryGetProperty("status", out _));
        Assert.Equal("group-raw", groupRaw.GetProperty("value").GetString());
        Assert.False(groupRaw.TryGetProperty("status", out _));
        Assert.True(groupWrapped.GetProperty("status").GetBoolean());
        Assert.Equal("group-wrapped", groupWrapped.GetProperty("data").GetProperty("value").GetString());
        Assert.True(single.GetProperty("status").GetBoolean());
        Assert.Equal("single", single.GetProperty("data").GetProperty("value").GetString());
    }

    private sealed record SampleResponse(string Value);
}
