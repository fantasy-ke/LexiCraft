using BuildingBlocks.Extensions;
using BuildingBlocks.MassTransit.Services;
using BuildingBlocks.Mediator;
using Fantasy.Services.Identity.Shared.Dtos;
using Fantasy.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace Fantasy.Services.Identity.Identity.Internal.Commands;

public record PublishLoginLogCommand(
    string UserAccount,
    string? Message,
    UserId? UserId = null,
    bool IsSuccess = false,
    string LoginType = "Password") : ICommand;

public class PublishLoginLogCommandHandler(
    IHttpContextAccessor httpContextAccessor,
    IEventPublisher eventPublisher) : ICommandHandler<PublishLoginLogCommand>
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

        await eventPublisher.PublishLocalAsync(logDto, cancellationToken);
    }
}