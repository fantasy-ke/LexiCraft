using BuildingBlocks.Extensions;
using BuildingBlocks.Serilog;
using LexiCraft.Files.Grpc.Data;
using LexiCraft.Files.Grpc.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using ProtoBuf.Grpc.Server;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile(path: "serilog.json", optional: false, reloadOnChange: true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

builder.AddServiceDefaults();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddGrpcHttpApi()
    .AddGrpcSwagger()
    .AddCodeFirstGrpc();
// builder.Services.WithScalar(new OpenApiInfo()
// {
//     Title = "词汇技艺 Files Api",
//     Version = "v1",
//     Description = "词汇技艺相关接口",
// });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo { Title = "file gRPC transcoding", Version = "v1" });
});
builder.Services.WithLexiCraftDbAccess(builder.Configuration);
builder.Services.WithMapster();
// builder.Services.AddDbContext<FilesDbContext>(opts =>
//     opts.UseSqlite(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = SerilogRequestUtility.HttpMessageTemplate;
    options.GetLevel = SerilogRequestUtility.GetRequestLevel;
    options.EnrichDiagnosticContext = SerilogRequestUtility.EnrichFromRequest;
});

var uploads = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(uploads),
    RequestPath = new PathString("/uploads"),
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "swagger";
    });
}

// Configure the HTTP request pipeline.
app.MapGrpcService<FilesService>();
app.UseMigration();
app.Run();