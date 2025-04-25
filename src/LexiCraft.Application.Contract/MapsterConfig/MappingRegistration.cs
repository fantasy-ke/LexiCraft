using Mapster;

namespace LexiCraft.Application.Contract.MapsterConfig;

public class MappingRegistration : IRegister
{
    void IRegister.Register(TypeAdapterConfig config)
    {
        // config.NewConfig<ModelA, ModelB>();
    }
}