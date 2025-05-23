using BuildingBlocks.Extensions;
using LexiCraft.Files.Grpc.Data;
using LexiCraft.Files.Grpc.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpcSwagger();
builder.Services.AddCodeFirstGrpc();
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