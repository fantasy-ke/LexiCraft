﻿using System.Text.Json;
using BuildingBlocks.Exceptions;
using LexiCraft.Application.Contract.Events;
using Z.EventBus;

namespace LexiCraft.Application.Contract.Exceptions;

public class AuthLoginException : UserFriendlyException
{
    public AuthLoginException()
    {
    }

    public AuthLoginException(string message) : base(message)
    {
    }
}

public static class ThrowAuthLoginException
{
    public static void ThrowException(IEventBus<LoginEto> loginEventBus, string message)
    {
        var exDto = JsonSerializer.Deserialize<LoginEto>(message);
        if (exDto != null) Task.Run(async () => await loginEventBus.PublishAsync(exDto));
        throw new AuthLoginException(exDto?.ErrorMessage ?? message);
    }
}