using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Interface.Base;
using BuildingBlocks.OSS.Interface.Service;
using Minio;

namespace BuildingBlocks.OSS.Services;

public partial class MinioOssService : BaseOSSService, IMinioOssService
{
    private readonly string _defaultPolicyVersion = "2012-10-17";

    public MinioOssService(ICacheProvider cache, OSSOptions options)
        : base(cache, options)
    {
        var client = new MinioClient()
            .WithEndpoint(options.Endpoint)
            .WithRegion(options.Region)
            .WithCredentials(options.AccessKey, options.SecretKey);
        if (options.IsEnableHttps) client = client.WithSSL();

        Context = (MinioClient)client.Build();
    }

    public MinioClient Context { get; }
}
