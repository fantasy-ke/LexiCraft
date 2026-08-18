using BuildingBlocks.Authentication.Permissions;

namespace Fantasy.Shared.Permissions;

/// <summary>
///     LexiCraft 全局权限定义。Identity.Api 使用同一份定义校验所有业务服务权限。
/// </summary>
public sealed class FantasyPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(PermissionDefinitionContext context)
    {
        var pages = context.CreatePermission(
            PermissionConstant.Pages,
            "页面访问",
            "所有页面权限的注册根节点");

        DefineIdentityPermissions(pages);
        DefinePracticePermissions(pages);
        DefineVocabularyPermissions(pages);
        DefineFilesPermissions(pages);
    }

    private static void DefineIdentityPermissions(PermissionDefinition pages)
    {
        var identity = pages.CreateChildPermission(
            IdentityPermissions.Page,
            "身份服务",
            "用户和权限管理相关权限");

        var users = identity.CreateChildPermission(
            IdentityPermissions.Users.Default,
            "用户管理",
            "用户管理相关权限");
        users.CreateChildPermission(IdentityPermissions.Users.Query, "查询用户", "允许查询用户信息");
        users.CreateChildPermission(IdentityPermissions.Users.Create, "创建用户", "允许创建新用户");
        users.CreateChildPermission(IdentityPermissions.Users.Edit, "编辑用户", "允许编辑用户信息");
        users.CreateChildPermission(IdentityPermissions.Users.Delete, "删除用户", "允许删除用户");
        users.CreateChildPermission(IdentityPermissions.Users.UploadAvatar, "上传头像", "允许上传用户头像");

        var permissions = identity.CreateChildPermission(
            IdentityPermissions.Permissions.Default,
            "权限管理",
            "权限分配相关权限");
        permissions.CreateChildPermission(IdentityPermissions.Permissions.Query, "查询权限", "允许查询用户权限");
        permissions.CreateChildPermission(IdentityPermissions.Permissions.Create, "新增权限", "允许为用户添加权限");
        permissions.CreateChildPermission(IdentityPermissions.Permissions.Update, "修改权限", "允许批量修改用户权限");
        permissions.CreateChildPermission(IdentityPermissions.Permissions.Delete, "删除权限", "允许删除用户权限");

        var events = identity.CreateChildPermission(
            IdentityPermissions.Events.Default,
            "事件管理",
            "失败事件查询与重放权限");
        events.CreateChildPermission(IdentityPermissions.Events.Replay, "重放事件", "允许重放失败事件");
    }

    private static void DefinePracticePermissions(PermissionDefinition pages)
    {
        var practice = pages.CreateChildPermission(
            PracticePermissions.Page,
            "练习服务",
            "练习服务相关权限");

        var tasks = practice.CreateChildPermission(
            PracticePermissions.Tasks.Default,
            "练习任务",
            "练习任务创建与管理");
        tasks.CreateChildPermission(PracticePermissions.Tasks.Create, "创建任务", "允许创建新的练习任务");
        tasks.CreateChildPermission(PracticePermissions.Tasks.Query, "查询任务", "允许查询练习任务详情");
        tasks.CreateChildPermission(PracticePermissions.Tasks.Complete, "完成任务", "允许完成练习任务");

        var assessments = practice.CreateChildPermission(
            PracticePermissions.Assessments.Default,
            "评估管理",
            "练习评估与记录管理");
        assessments.CreateChildPermission(PracticePermissions.Assessments.Create, "创建评估", "允许创建练习评估记录");
        assessments.CreateChildPermission(PracticePermissions.Assessments.Query, "查询评估", "允许查询评估记录");
        assessments.CreateChildPermission(PracticePermissions.Assessments.Update, "更新评估", "允许更新评估记录");
        assessments.CreateChildPermission(PracticePermissions.Assessments.Submit, "提交评估", "允许提交练习评估");
    }

    private static void DefineVocabularyPermissions(PermissionDefinition pages)
    {
        var vocabulary = pages.CreateChildPermission(
            VocabularyPermissions.Page,
            "词汇服务",
            "词汇服务相关权限");

        var words = vocabulary.CreateChildPermission(
            VocabularyPermissions.Words.Default,
            "单词管理",
            "单词检索与导入管理");
        words.CreateChildPermission(VocabularyPermissions.Words.Query, "查询单词", "允许查询和检索单词详情");
        words.CreateChildPermission(VocabularyPermissions.Words.Import, "导入单词", "允许通过 JSON 批量导入单词");

        var wordLists = vocabulary.CreateChildPermission(
            VocabularyPermissions.WordLists.Default,
            "词库管理",
            "词库列表与分类管理");
        wordLists.CreateChildPermission(VocabularyPermissions.WordLists.Query, "查询词库", "允许查询词库列表及其分类");

        var userStates = vocabulary.CreateChildPermission(
            VocabularyPermissions.UserStates.Default,
            "学习状态",
            "用户学习进度与弱词分析");
        userStates.CreateChildPermission(VocabularyPermissions.UserStates.Query, "查询学习状态", "允许查询用户学习数据及弱词分析");
        userStates.CreateChildPermission(VocabularyPermissions.UserStates.Update, "更新学习状态", "允许记录用户学习进度");
    }

    private static void DefineFilesPermissions(PermissionDefinition pages)
    {
        var files = pages.CreateChildPermission(
            FilesPermissions.Page,
            "文件服务",
            "文件查询、读取和管理权限");

        var items = files.CreateChildPermission(
            FilesPermissions.Items.Default,
            "文件管理",
            "文件与文件夹访问权限");
        items.CreateChildPermission(FilesPermissions.Items.Query, "查询文件", "允许查询文件元数据和目录树");
        items.CreateChildPermission(FilesPermissions.Items.ReadContent, "读取文件", "允许读取文件内容");
        items.CreateChildPermission(FilesPermissions.Items.Upload, "上传文件", "允许上传单个或批量文件");
        items.CreateChildPermission(FilesPermissions.Items.CreateFolder, "创建文件夹", "允许创建文件夹");
        items.CreateChildPermission(FilesPermissions.Items.Delete, "删除文件", "允许删除文件或文件夹");
    }
}
