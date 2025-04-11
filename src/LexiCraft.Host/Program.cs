using LexiCraft.Infrastructure.Extensions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.WithScalar(new OpenApiInfo()
{
    Title = "词汇技艺 Web Api",
    Version = "v1",
    Description = "词汇技艺相关接口",
})
.WithJwt(builder.Configuration)
.WithLexiCraftDbAccess(builder.Configuration)
.WithRedis(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();

builder.Services.WithIdGen();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapFast();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseScalar("EarthChat Auth Server");
}

app.UseHttpsRedirection();


await app.RunAsync();
