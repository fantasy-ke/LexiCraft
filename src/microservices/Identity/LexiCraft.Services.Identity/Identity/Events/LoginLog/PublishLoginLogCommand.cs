using BuildingBlocks.Extensions;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Z.EventBus;

namespace LexiCraft.Services.Identity.Identity.Events.LoginLog;

public record PublishLoginLogCommand(
    string UserAccount,
    string? ErrorMessage,
    Guid? UserId = null,
    bool IsSuccess = false,
    string LoginType = "Password") : ICommand;

public class PublishLoginLogCommandHandler(
    IHttpContextAccessor httpContextAccessor,
    IEventBus<LoginLogDto> loginEventBus) : ICommandHandler<PublishLoginLogCommand>
{
    public async Task Handle(PublishLoginLogCommand command, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        var ip = context?.GetRequestIp();
        var userAgent = context?.Request.Headers["User-Agent"].ToString();
        var origin = context?.Request.Headers["Origin"].ToString();

        var logDto = new LoginLogDto(
            command.UserId,
            command.UserAccount,
            DateTime.Now,
            origin,
            ip,
            userAgent,
            command.LoginType,
            command.IsSuccess,
            command.ErrorMessage);

        await loginEventBus.PublishAsync(logDto);
    }
}
