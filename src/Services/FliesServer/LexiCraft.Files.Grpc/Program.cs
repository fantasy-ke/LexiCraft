using BuildingBlocks.Extensions;
using LexiCraft.Files.Grpc.Data;
using LexiCraft.Files.Grpc.Services;
using Microsoft.Extensions.FileProviders;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCodeFirstGrpc();
// builder.Services.WithScalar(new OpenApiInfo()
// {
//     Title = "词汇技艺 Files Api",
//     Version = "v1",
//     Description = "词汇技艺相关接口",
// });
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
// Configure the HTTP request pipeline.
app.MapGrpcService<FilesService>();
app.UseMigration();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");


app.Run();