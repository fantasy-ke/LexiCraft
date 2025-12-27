using System.Text.Json;
using BuildingBlocks.Exceptions;
using LexiCraft.AuthServer.Application.Contract.Events;
using Z.EventBus;

namespace LexiCraft.AuthServer.Application.Contract.Exceptions;

public class AuthLoginException(string message) : UserFriendlyException(message);

public static class ThrowAuthLoginException
{
    public static void ThrowException(IEventBus<LoginEto> loginEventBus, string message)
    {
        var exDto = JsonSerializer.Deserialize<LoginEto>(message);
        if (exDto != null) Task.Run(async () => await loginEventBus.PublishAsync(exDto));
        throw new AuthLoginException(exDto?.ErrorMessage ?? message);
    }
}