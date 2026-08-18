namespace Fantasy.Shared.Permissions;

public static class PermissionConstant
{
    public const string Admin = "admin";
    public const string User = "user";
    public const string Pages = "Pages";

    /// <summary>
    ///     默认用户权限
    /// </summary>
    public static class DefaultUserPermissions
    {
        public static readonly string[] Permissions =
        [
            Pages,
            IdentityPermissions.Page,
            IdentityPermissions.Users.Default,
            IdentityPermissions.Users.Query,
            IdentityPermissions.Users.UploadAvatar,
            PracticePermissions.Page,
            PracticePermissions.Tasks.Default,
            PracticePermissions.Tasks.Create,
            PracticePermissions.Tasks.Complete,
            PracticePermissions.Assessments.Default,
            PracticePermissions.Assessments.Submit,
            VocabularyPermissions.Page,
            VocabularyPermissions.Words.Default,
            VocabularyPermissions.Words.Query,
            VocabularyPermissions.WordLists.Default,
            VocabularyPermissions.WordLists.Query,
            VocabularyPermissions.UserStates.Default,
            VocabularyPermissions.UserStates.Query,
            VocabularyPermissions.UserStates.Update,
            FilesPermissions.Page,
            FilesPermissions.Items.Default,
            FilesPermissions.Items.Query,
            FilesPermissions.Items.ReadContent
        ];
    }
}