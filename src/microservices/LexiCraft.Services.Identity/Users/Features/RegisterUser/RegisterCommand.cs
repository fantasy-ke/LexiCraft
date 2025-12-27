using System.Text.RegularExpressions;
using BuildingBlocks.Mediator;
using FluentValidation;
using Lazy.Captcha.Core;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Dtos;
using LexiCraft.Services.Identity.Shared.Exceptions;
using Z.EventBus;

namespace LexiCraft.Services.Identity.Users.Features.RegisterUser;

public record RegisterCommand(string UserAccount, string Email, string Password, string CaptchaKey, string CaptchaCode)
    : ICommand<bool>;

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

public partial class RegisterCommandHandler(
    IUserRepository userRepository,
    IUserPermissionRepository userPermissionRepository,
    IEventBus<LoginLogDto> loginEventBus,
    ICaptcha captcha)
    : ICommandHandler<RegisterCommand, bool>
{
    public async Task<bool> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // 验证验证码
        if (!captcha.Validate(command.CaptchaKey, command.CaptchaCode))
        {
            var loginLogDto = new LoginLogDto(null, command.UserAccount, null, DateTime.Now,
                null, null, null, "Register", false, "验证码不正确");

            ThrowIdentityAuthException.ThrowException(loginEventBus, System.Text.Json.JsonSerializer.Serialize(loginLogDto));

        }

        // 检查用户账号是否已存在
        var any = await userRepository.AnyAsync(p => p.UserAccount == command.UserAccount);
        if (any)
        {
            var loginLogDto = new LoginLogDto(null, command.UserAccount, null, DateTime.Now,
                null, null, null, "Register", false, "当前用户名已存在，请重新输入");

            ThrowIdentityAuthException.ThrowException(loginEventBus, System.Text.Json.JsonSerializer.Serialize(loginLogDto));

        }

        try
        {
            // 创建用户
            var user = new User(command.UserAccount, command.Email);
            user.SetPassword(command.Password);
            user.Avatar = "🦜";
            user.Roles.Add(PermissionConstant.User);
            user.UpdateLastLogin();
            user.UpdateSource(SourceEnum.Register);

            var afterUser = await userRepository.InsertAsync(user);
            await userRepository.SaveChangesAsync();

            // 为用户分配默认权限
            var defaultPermissions = PermissionConstant.DefaultUserPermissions.Permissions;
            await userPermissionRepository.AddUserPermissionsAsync(afterUser.Id, defaultPermissions);

            return true;
        }
        catch (Exception e)
        {
            throw new Exception($"{e.Message}用户注册失败", e);
        }
    }

    [GeneratedRegex("^(?=.*[0-9])(?=.*[a-zA-Z]).*$")]
    private static partial Regex PasswordRegex();

    [GeneratedRegex(@"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$")]
    private static partial Regex EmailRegex();
}