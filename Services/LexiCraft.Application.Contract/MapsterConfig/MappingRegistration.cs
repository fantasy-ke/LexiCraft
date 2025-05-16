using LexiCraft.Application.Contract.Files.Dtos;
using LexiCraft.Domain.Files;
using Mapster;

namespace LexiCraft.Application.Contract.MapsterConfig;

public class MappingRegistration : IRegister
{
    void IRegister.Register(TypeAdapterConfig config)
    {
        config.NewConfig<FileInfos, FileInfoDto>()
            .Ignore(dest => dest.Children); // 子项需要手动映射，避免无限递归
        // config.NewConfig<ModelA, ModelB>();
    }
}