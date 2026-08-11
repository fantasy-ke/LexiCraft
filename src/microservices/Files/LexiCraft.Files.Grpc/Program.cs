using BuildingBlocks.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.OSS;
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;
using BuildingBlocks.SerilogLogging.Extensions;
using BuildingBlocks.SerilogLogging.Utils;
using LexiCraft.Files.Grpc.Data;
using LexiCraft.Files.Grpc.HttpApi;
using LexiCraft.Files.Grpc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using ProtoBuf.Grpc.Server;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.AddServiceDefaults();
builder.AddOssService();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Add services to the container.
builder.Services.AddCodeFirstGrpc(options => { options.EnableDetailedErrors = true; });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthentication();
builder.AddAspnetOpenApi();
builder.Services.WithLexiCraftDbAccess(builder.Configuration);
builder.Services.WithMapster();
builder.Services.AddScoped<IFilesService, FilesService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = SerilogRequestUtility.HttpMessageTemplate;
    options.GetLevel = SerilogRequestUtility.GetRequestLevel;
    options.EnrichDiagnosticContext = SerilogRequestUtility.EnrichFromRequest;
});

var uploads = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploads),
    RequestPath = new PathString("/uploads")
});

app.MapGet("/content", async ([FromQuery] string relativePath, [FromServices] IFilesService filesService) =>
    {
        var fileResponse = await filesService.GetFileByPathAsync(relativePath);
        return Results.File(fileResponse.FileStream, fileResponse.ContentType, fileResponse.FileName);
    })
    .ExcludeFromDescription();

app.MapFilesApiEndpoints();

if (app.Environment.IsDevelopment()) app.UseAspnetOpenApi();

// Configure the HTTP request pipeline.
app.MapGrpcService<FilesService>();
app.UseMigration();
app.Run();