using LexiCraft.Application.Contract.Files.Dtos;
using LexiCraft.Domain.Files;
using Mapster;

namespace LexiCraft.Application.Contract.MapsterConfig;

/// <summary>
/// Mapster映射配置
/// </summary>
public static class MapsterConfiguration
{
    public static void AddMapsterConfig()
    {
        // FileInfo -> FileInfoDto 映射配置
        TypeAdapterConfig<FileInfos, FileInfoDto>.NewConfig()
            .Ignore(dest => dest.Children); // 子项需要手动映射，避免无限递归
    }
} 