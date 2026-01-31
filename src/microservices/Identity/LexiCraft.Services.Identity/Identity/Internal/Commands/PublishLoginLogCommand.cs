using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Extensions;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Shared.Dtos;
using LexiCraft.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace LexiCraft.Services.Identity.Identity.Internal.Commands;

public record PublishLoginLogCommand(
    string UserAccount,
    string? Message,
    UserId? UserId = null,
    bool IsSuccess = false,
    string LoginType = "Password") : ICommand;

public class PublishLoginLogCommandHandler(
    IHttpContextAccessor httpContextAccessor,
    IEventBus<LoginLogEvent> loginEventBus) : ICommandHandler<PublishLoginLogCommand>
{
    public async Task Handle(PublishLoginLogCommand command, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        var ip = context?.GetRequestIp();
        var userAgent = context?.Request.Headers["User-Agent"].ToString();
        var origin = context?.Request.Headers["Origin"].ToString();

        var logDto = new LoginLogEvent(
            command.UserId?.Value,
            command.UserAccount,
            DateTime.Now,
            origin,
            ip,
            userAgent,
            command.LoginType,
            command.IsSuccess,
            command.Message);

        await loginEventBus.PublishAsync(logDto);
    }
}