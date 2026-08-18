using System.Text;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Options;
using BuildingBlocks.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.OSS;
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;
using BuildingBlocks.SerilogLogging.Extensions;
using BuildingBlocks.SerilogLogging.Utils;
using Fantasy.Files.Grpc.Data;
using Fantasy.Files.Grpc.HttpApi;
using Fantasy.Files.Grpc.Options;
using Fantasy.Files.Grpc.Services;
using Fantasy.Shared.Permissions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using ProtoBuf.Grpc.Server;
using Serilog;
var builder = WebApplication.CreateBuilder(args);
builder.AddSerilogLogging();
builder.AddServiceDefaults();
builder.AddOssService();
builder.Services.Configure<FilesStorageCompatibilityOptions>(
    builder.Configuration.GetSection(FilesStorageCompatibilityOptions.SectionName));
var oauthOptions = builder.Configuration.BindOptions<OAuthOptions>();
if (string.IsNullOrWhiteSpace(oauthOptions.Secret))
    throw new InvalidOperationException("OAuthOptions:Secret must be provided by AgileConfig or environment variables");
builder.RegisterAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Audience = oauthOptions.Audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = oauthOptions.ValidateIssuer,
            ValidIssuers = oauthOptions.ValidIssuers,
            ValidateAudience = oauthOptions.ValidateAudience,
            ValidAudiences = oauthOptions.ValidAudiences,
            ValidateLifetime = oauthOptions.ValidateLifetime,
            ClockSkew = oauthOptions.ClockSkew,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(oauthOptions.Secret))
        };
        options.MapInboundClaims = false;
    });
builder.Services.AddPermissionDefinitionProvider<FantasyPermissionDefinitionProvider>();
builder.Services.AddIdentityApiPermissionValidation();
builder.Services.AddCodeFirstGrpc(options => { options.EnableDetailedErrors = true; });
builder.Services.AddEndpointsApiExplorer();
builder.AddAspnetOpenApi();
builder.Services.AddFantasyDbAccess(builder.Configuration);
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
app.UseAuthentication();
app.UseAuthorization();
var uploads = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploads),
    RequestPath = new PathString("/uploads")
});

// 兼容现有头像和公开资源 URL；需要私有文件时应使用已鉴权的版本化 content 端点。
app.MapGet("/content", async ([FromQuery] string relativePath, [FromServices] IFilesService filesService) =>
    {
        var fileResponse = await filesService.GetFileByPathAsync(relativePath);
        return Results.File(fileResponse.FileStream, fileResponse.ContentType, fileResponse.FileName);
    })
    .ExcludeFromDescription();

app.MapFilesApiEndpoints();
if (app.Environment.IsDevelopment()) app.UseAspnetOpenApi();
app.MapGrpcService<FilesService>();
await app.UseMigrationAsync();
app.Run();