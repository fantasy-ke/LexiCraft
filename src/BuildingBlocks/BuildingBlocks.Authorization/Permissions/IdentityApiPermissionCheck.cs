using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Authentication.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace BuildingBlocks.Authentication.Permissions;

/// <summary>
///     将业务服务当前请求的 Bearer Token 和权限集合转发给 Identity API 进行集中验证。
/// </summary>
internal sealed class IdentityApiPermissionCheck(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptionsMonitor<PermissionAuthorizationOptions> options,
    ILogger<IdentityApiPermissionCheck> logger) : IPermissionCheck
{
    /// <inheritdoc />
    public async Task<PermissionValidationResult> CheckAsync(
        IReadOnlyCollection<string> permissionNames,
        CancellationToken cancellationToken = default)
    {
        var requiredPermissions = permissionNames
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var authorizationHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var parsedHeader) ||
            !string.Equals(parsedHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(parsedHeader.Parameter))
        {
            return PermissionValidationResult.InvalidSession;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            options.CurrentValue.IdentityApiValidationPath)
        {
            Content = JsonContent.Create(new PermissionValidationRequest(requiredPermissions))
        };
        request.Headers.Authorization = parsedHeader;

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return PermissionValidationResult.InvalidSession;

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Identity permission validation returned {StatusCode} for {PermissionCount} permissions",
                    (int)response.StatusCode,
                    requiredPermissions.Length);
                return PermissionValidationResult.Unavailable;
            }

            var result = await response.Content.ReadFromJsonAsync<PermissionValidationResult>(
                cancellationToken: cancellationToken);
            return result ?? PermissionValidationResult.Unavailable;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Identity permission validation failed for {PermissionCount} permissions",
                requiredPermissions.Length);
            return PermissionValidationResult.Unavailable;
        }
    }
}