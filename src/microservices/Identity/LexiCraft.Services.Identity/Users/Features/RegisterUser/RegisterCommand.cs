using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using Lazy.Captcha.Core;
using LexiCraft.Services.Identity.Identity.Internal.Commands;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Dtos;
using LexiCraft.Services.Identity.Users.Internal.Commands;
using MediatR;

namespace LexiCraft.Services.Identity.Users.Features.RegisterUser;

public record RegisterCommand(string UserAccount, string Email, string Password, string CaptchaKey, string CaptchaCode)
    : ICommand<TokenResponse>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.UserAccount)
            .NotEmpty().WithMessage("请输入账号")
            .MaximumLength(50).WithMessage("账号长度不能超过50个字符");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("请输入邮箱")
            .EmailAddress().WithMessage("邮箱格式不正确");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("请输入密码")
            .MinimumLength(6).WithMessage("密码长度至少6位")
            .Matches("^(?=.*[0-9])(?=.*[a-zA-Z]).*$").WithMessage("密码必须包含字母和数字");

        RuleFor(x => x.CaptchaKey)
            .NotEmpty().WithMessage("验证码Key不能为空");

        RuleFor(x => x.CaptchaCode)
            .NotEmpty().WithMessage("请输入验证码");
    }
}

public class RegisterCommandHandler(
    IUserRepository userRepository,
    ICaptcha captcha,
    IMediator mediator)
    : ICommandHandler<RegisterCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // 验证验证码
        if (!captcha.Validate(command.CaptchaKey, command.CaptchaCode))
        {
            await mediator.Send(new PublishLoginLogCommand(command.UserAccount, "验证码不正确", LoginType: "Register"),
                cancellationToken);
            ThrowUserFriendlyException.ThrowException("验证码不正确");
        }

        // 检查用户账号是否已存在
        var any = await userRepository.AnyAsync(p => p.UserAccount == command.UserAccount);
        if (any)
        {
            await mediator.Send(
                new PublishLoginLogCommand(command.UserAccount, "当前用户名已存在，请重新输入", LoginType: "Register"),
                cancellationToken);
            ThrowUserFriendlyException.ThrowException("当前用户名已存在，请重新输入");
        }

        try
        {
            // 使用 CreateUserCommand 创建用户
            var user = await mediator.Send(new CreateUserCommand(
                command.UserAccount,
                command.Email,
                command.Password,
                SourceEnum.Register
            ), cancellationToken);

            // 注册成功后自动登录逻辑
            return await mediator.Send(new GenerateTokenResponseCommand(user, "注册成功并登录"), cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception($"{e.Message}用户注册失败", e);
        }
    }
}