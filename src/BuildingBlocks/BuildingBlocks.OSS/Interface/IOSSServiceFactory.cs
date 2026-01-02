using Minio;
using BuildingBlocks.OSS.Interface.Service;

namespace BuildingBlocks.OSS.Interface
{
    public interface IOSSServiceFactory<T>
    {
        IOSSService<T> Create();
    }
}