namespace LexiCraft.Shared.Permissions;

/// <summary>
/// 练习服务相关权限定义
/// </summary>
public static class PracticePermissions
{
    public const string Page = "Pages.Practice";

    public static class Tasks
    {
        public const string Default = "Pages.Practice.Tasks";
        public const string Create = "Pages.Practice.Tasks.Create";
        public const string Query = "Pages.Practice.Tasks.Query";
        public const string Complete = "Pages.Practice.Tasks.Complete";
    }

    public static class Assessments
    {
        public const string Default = "Pages.Practice.Assessments";
        public const string Create = "Pages.Practice.Assessments.Create";
        public const string Query = "Pages.Practice.Assessments.Query";
        public const string Update = "Pages.Practice.Assessments.Update";
        public const string Submit = "Pages.Practice.Assessments.Submit";
    }
}