namespace LexiCraft.Shared.Permissions;

/// <summary>
///     词汇服务相关权限定义
/// </summary>
public static class VocabularyPermissions
{
    public const string Page = "Pages.Vocabulary";

    public static class Words
    {
        public const string Default = "Pages.Vocabulary.Words";
        public const string Query = "Pages.Vocabulary.Words.Query";
        public const string Import = "Pages.Vocabulary.Words.Import";
    }

    public static class WordLists
    {
        public const string Default = "Pages.Vocabulary.WordLists";
        public const string Query = "Pages.Vocabulary.WordLists.Query";
    }

    public static class UserStates
    {
        public const string Default = "Pages.Vocabulary.UserStates";
        public const string Query = "Pages.Vocabulary.UserStates.Query";
        public const string Update = "Pages.Vocabulary.UserStates.Update";
    }
}