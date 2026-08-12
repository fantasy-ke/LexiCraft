namespace LexiCraft.Shared.Permissions;

/// <summary>
///     文件服务相关权限定义。
/// </summary>
public static class FilesPermissions
{
    public const string Page = "Pages.Files";

    public static class Items
    {
        public const string Default = "Pages.Files.Items";
        public const string Query = "Pages.Files.Items.Query";
        public const string ReadContent = "Pages.Files.Items.ReadContent";
        public const string Upload = "Pages.Files.Items.Upload";
        public const string CreateFolder = "Pages.Files.Items.CreateFolder";
        public const string Delete = "Pages.Files.Items.Delete";
    }
}
