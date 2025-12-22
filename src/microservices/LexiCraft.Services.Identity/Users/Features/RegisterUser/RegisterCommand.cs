using System.Text.RegularExpressions;
using BuildingBlocks.Mediator;
using Lazy.Captcha.Core;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared;
using LexiCraft.Services.Identity.Shared.Contracts;

namespace LexiCraft.Services.Identity.Users.Features.RegisterUser;

public record RegisterCommand(string UserAccount, string Email, string Password, string CaptchaKey, string CaptchaCode)
    : ICommand<bool>;

public partial class RegisterCommandHandler(
    IUserRepository userRepository,
    IUserPermissionRepository userPermissionRepository,
    ICaptcha captcha)
    : ICommandHandler<RegisterCommand, bool>
{
    public async Task<bool> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // 验证邮箱格式
        if (string.IsNullOrEmpty(command.Email) || !EmailRegex().IsMatch(command.Email))
        {
            throw new Exception("邮箱格式不正确");
        }

        // 验证密码强度
        if (string.IsNullOrEmpty(command.Password)
            || command.Password.Length < 6 ||
            !PasswordRegex().IsMatch(command.Password))
        {
            throw new Exception("密码长度至少6位，且必须包含字母和数字");
        }

        // 验证验证码相关信息
        if (string.IsNullOrEmpty(command.CaptchaKey) || string.IsNullOrEmpty(command.CaptchaCode))
        {
            throw new Exception("请输入验证码");
        }

        // 验证验证码
        if (!captcha.Validate(command.CaptchaKey, command.CaptchaCode))
        {
            throw new Exception("验证码校验错误");
        }

        // 验证用户账号
        if (string.IsNullOrEmpty(command.UserAccount))
        {
            throw new Exception("请输入账号");
        }

        // 检查用户账号是否已存在
        var any = await userRepository.AnyAsync(p => p.UserAccount == command.UserAccount);
        if (any)
        {
            throw new Exception("当前用户名已存在，请重新输入");
        }

        try
        {
            // 创建用户
            var user = new User(command.UserAccount, command.Email);
            user.SetPassword(command.Password);
            user.Avatar = "🦜";
            user.Roles.Add(RoleConstant.User);
            user.UpdateLastLogin();
            user.UpdateSource(SourceEnum.Register);

            var afterUser = await userRepository.InsertAsync(user);
            await userRepository.SaveChangesAsync();

            // 为用户分配默认权限
            var defaultPermissions = RoleConstant.DefaultUserPermissions.Permissions;
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