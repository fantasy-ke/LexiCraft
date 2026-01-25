using FluentValidation;
using LexiCraft.Services.Identity.Shared.Authorize;
using MediatR;
using Serilog;

namespace LexiCraft.Services.Identity.Identity.Features.OAuthInitiate;

/// <summary>
///     OAuth初始化查询
/// </summary>
/// <param name="Provider">OAuth提供商类型（github/gitee）</param>
/// <param name="RedirectUri">授权成功后的回调地址</param>
/// <param name="State">状态参数，用于防止CSRF攻击</param>
public record OAuthInitiateQuery(string Provider) : IRequest<string>;

public class OAuthInitiateQueryValidator : AbstractValidator<OAuthInitiateQuery>
{
    public OAuthInitiateQueryValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("OAuth提供商类型不能为空")
            .Must(BeValidOAuthType).WithMessage("不支持的OAuth提供者类型");
    }

    private bool BeValidOAuthType(string type)
    {
        var validTypes = new[] { "github", "gitee" };
        return validTypes.Contains(type.ToLower());
    }

    private bool BeValidUri(string uri)
    {
        return Uri.TryCreate(uri, UriKind.Absolute, out _);
    }
}

internal class OAuthInitiateQueryHandler(OAuthProviderFactory oauthProviderFactory)
    : IRequestHandler<OAuthInitiateQuery, string>
{
    public Task<string> Handle(OAuthInitiateQuery request, CancellationToken cancellationToken)
    {
        // 获取对应的OAuth提供者
        var provider = oauthProviderFactory.GetProvider(request.Provider);
        if (provider is null) throw new InvalidOperationException($"不支持的OAuth提供者: {request.Provider}");

        // 生成state参数（如果未提供）
        var state = Guid.NewGuid().ToString("N");

        // 获取授权URL
        var authorizationUrl = provider.GetAuthorizationUrl(state);

        Log.Logger.Information("获取OAuth授权URL: {AuthorizationUrl}", authorizationUrl);
        return Task.FromResult(authorizationUrl);
    }
}