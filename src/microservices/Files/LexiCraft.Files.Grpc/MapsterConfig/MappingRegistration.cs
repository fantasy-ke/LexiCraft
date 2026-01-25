using BuildingBlocks.Grpc.Contracts.FileGrpc;
using LexiCraft.Files.Grpc.Model;
using Mapster;

namespace LexiCraft.Files.Grpc.MapsterConfig;

/// <summary>
///     映射注册
/// </summary>
public class MappingRegistration : IRegister
{
    void IRegister.Register(TypeAdapterConfig config)
    {
        config.NewConfig<FileInfos, FileInfoDto>()
            .Ignore(dest => dest.Children!); // 子项需要手动映射，避免无限递归
        // config.NewConfig<ModelA, ModelB>();
    }
}