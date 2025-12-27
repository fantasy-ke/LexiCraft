using System.Text.Json;
using BuildingBlocks.Exceptions;
using LexiCraft.Services.Identity.Shared.Dtos;
using Z.EventBus;

namespace LexiCraft.Services.Identity.Shared.Exceptions;

public class IdentityAuthException : UserFriendlyException
{
    public IdentityAuthException()
    {
    }

    public IdentityAuthException(string message) : base(message)
    {
    }
}

public static class ThrowIdentityAuthException
{
    public static void ThrowException(IEventBus<LoginLogDto> loginEventBus, string message)
    {
        var exDto = JsonSerializer.Deserialize<LoginLogDto>(message);
        if (exDto != null) Task.Run(async () => await loginEventBus.PublishAsync(exDto));
        throw new IdentityAuthException(exDto?.ErrorMessage ?? message);
    }
}