namespace LexiCraft.Shared.Permissions;

public static class PracticePermissions
{
    public const string Page = "Pages.Practice";

    public static class Tasks
    {
        public const string Default = "Pages.Practice.Tasks";
        public const string Generate = "Pages.Practice.Tasks.Generate";
    }

    public static class Answers
    {
        public const string Default = "Pages.Practice.Answers";
        public const string Submit = "Pages.Practice.Answers.Submit";
    }

    public static class History
    {
        public const string Default = "Pages.Practice.History";
        public const string Query = "Pages.Practice.History.Query";
    }

    public static class Mistakes
    {
        public const string Default = "Pages.Practice.Mistakes";
        public const string Query = "Pages.Practice.Mistakes.Query";
    }

    public static class Performance
    {
        public const string Default = "Pages.Practice.Performance";
        public const string Query = "Pages.Practice.Performance.Query";
    }
}