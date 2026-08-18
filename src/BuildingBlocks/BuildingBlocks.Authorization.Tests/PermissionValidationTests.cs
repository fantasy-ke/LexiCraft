using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Authentication.Permissions;
using BuildingBlocks.Authentication.Options;
using Fantasy.Shared.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BuildingBlocks.Authentication.Policies;

namespace BuildingBlocks.Authorization.Tests;

public sealed class PermissionValidationTests
{
    [Fact]
    public void SharedProvider_RegistersOneCompletePermissionTree()
    {
        var manager = CreatePermissionDefinitionManager();

        Assert.Single(manager.GetRootPermissions());
        Assert.Equal(PermissionConstant.Pages, manager.GetRootPermissions()[0].Name);
        Assert.Contains(manager.GetPermissions(), item => item.Name == IdentityPermissions.Permissions.Update);
        Assert.Contains(manager.GetPermissions(), item => item.Name == IdentityPermissions.Events.Replay);
        Assert.Contains(manager.GetPermissions(), item => item.Name == PracticePermissions.Tasks.Complete);
        Assert.Contains(manager.GetPermissions(), item => item.Name == VocabularyPermissions.UserStates.Update);
        Assert.Contains(manager.GetPermissions(), item => item.Name == FilesPermissions.Items.ReadContent);
        Assert.Equal(
            manager.GetPermissions().Count,
            manager.GetPermissions().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task PermissionCheck_RequiresExactPermissionInsteadOfParentInheritance()
    {
        var userId = Guid.NewGuid();
        var check = new PermissionCheck(
            new TestUserContext { UserId = userId, IsAuthenticated = true },
            new TestUserPermissionStore(PermissionConstant.Pages),
            CreatePermissionDefinitionManager(),
            new TestOptionsMonitor<PermissionAuthorizationOptions>(new PermissionAuthorizationOptions()));

        var result = await check.CheckAsync([VocabularyPermissions.Words.Query]);

        Assert.False(result.Granted);
        Assert.True(result.SessionValid);
        Assert.True(result.ServiceAvailable);
        Assert.Equal([VocabularyPermissions.Words.Query], result.MissingPermissions);
    }

    [Fact]
    public async Task PermissionCheck_AllowsExactPermissionAndAdministratorRole()
    {
        var manager = CreatePermissionDefinitionManager();
        var options = new TestOptionsMonitor<PermissionAuthorizationOptions>(new PermissionAuthorizationOptions());
        var exactCheck = new PermissionCheck(
            new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true },
            new TestUserPermissionStore(PracticePermissions.Tasks.Create),
            manager,
            options);
        var adminCheck = new PermissionCheck(
            new TestUserContext
            {
                UserId = Guid.NewGuid(),
                IsAuthenticated = true,
                Roles = [PermissionConstant.Admin]
            },
            new TestUserPermissionStore(),
            manager,
            options);

        Assert.True((await exactCheck.CheckAsync([PracticePermissions.Tasks.Create])).Granted);
        Assert.True((await adminCheck.CheckAsync([VocabularyPermissions.Words.Import])).Granted);

        var unknownPermission = await adminCheck.CheckAsync(["Pages.Unknown.Action"]);
        Assert.False(unknownPermission.Granted);
        Assert.Equal(["Pages.Unknown.Action"], unknownPermission.MissingPermissions);
    }

    [Fact]
    public void DefaultUserPermissions_ContainEveryNormalUserEndpointPermission()
    {
        var defaults = PermissionConstant.DefaultUserPermissions.Permissions.ToHashSet(StringComparer.Ordinal);
        var expected = new[]
        {
            IdentityPermissions.Users.UploadAvatar,
            PracticePermissions.Tasks.Create,
            PracticePermissions.Tasks.Complete,
            PracticePermissions.Assessments.Submit,
            VocabularyPermissions.Words.Query,
            VocabularyPermissions.WordLists.Query,
            VocabularyPermissions.UserStates.Query,
            VocabularyPermissions.UserStates.Update,
            FilesPermissions.Items.Query,
            FilesPermissions.Items.ReadContent
        };

        Assert.All(expected, permission => Assert.Contains(permission, defaults));
        Assert.DoesNotContain(VocabularyPermissions.Words.Import, defaults);
        Assert.DoesNotContain(IdentityPermissions.Permissions.Update, defaults);
        Assert.DoesNotContain(FilesPermissions.Items.Delete, defaults);
    }

    [Fact]
    public async Task AuthorizationPolicyProvider_DelegatesConfiguredPoliciesAndRejectsUnknownPermissions()
    {
        var authorizationOptions = new Microsoft.AspNetCore.Authorization.AuthorizationOptions();
        authorizationOptions.AddPolicy("configured", policy => policy.RequireClaim("scope", "test"));
        authorizationOptions.AddPolicy("configured-permission", policy =>
            policy.AddRequirements(new AuthorizeRequirement(VocabularyPermissions.Words.Query)));
        var provider = new AuthorizationPolicyProvider(
            Options.Create(authorizationOptions),
            CreatePermissionDefinitionManager());

        var configured = await provider.GetPolicyAsync("configured");
        var configuredPermission = await provider.GetPolicyAsync("configured-permission");
        var permission = await provider.GetPolicyAsync(VocabularyPermissions.Words.Query);
        var unknown = await provider.GetPolicyAsync("Pages.Unknown.Action");

        Assert.NotNull(configured);
        Assert.Contains(configured.Requirements, requirement =>
            requirement is AuthorizeRequirement authorizeRequirement &&
            authorizeRequirement.AuthorizeName.Length == 0);
        Assert.NotNull(configuredPermission);
        Assert.Single(configuredPermission.Requirements.OfType<AuthorizeRequirement>());
        Assert.NotNull(permission);
        Assert.Contains(permission.Requirements, requirement =>
            requirement is AuthorizeRequirement authorizeRequirement &&
            authorizeRequirement.AuthorizeName.SequenceEqual([VocabularyPermissions.Words.Query]));
        Assert.Null(unknown);
    }

    private static IPermissionDefinitionManager CreatePermissionDefinitionManager()
    {
        return new PermissionDefinitionManager([new FantasyPermissionDefinitionProvider()]);
    }
}
