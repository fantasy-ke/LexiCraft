using BuildingBlocks.Authentication.Permission;
using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Vocabulary.Shared.Authorization;

/// <summary>
/// 词汇服务权限定义提供程序
/// </summary>
public class VocabularyPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(PermissionDefinitionContext context)
    {
        // 获取或创建根页面权限 (与 Identity 服务对齐使用 "Pages")
        var pages = context.GetPermissionOrNull(PermissionConstant.Pages) ??
                    context.CreatePermission(PermissionConstant.Pages, "页面访问", "所有页面访问权限");

        // 创建词汇服务模块权限
        var vocabularyPage = pages.GetChildOrNull(VocabularyPermissions.Page) ??
                                 pages.CreateChildPermission(VocabularyPermissions.Page, "词汇服务", "词汇服务相关权限");

        // --- 单词管理 ---
        var wordsGroup = vocabularyPage.CreateChildPermission(VocabularyPermissions.Words.Default, "单词管理", "单词检索与导入管理");
        wordsGroup.CreateChildPermission(VocabularyPermissions.Words.Query, "查询单词", "允许查询和检索单词详情");
        wordsGroup.CreateChildPermission(VocabularyPermissions.Words.Import, "导入单词", "允许通过 JSON 批量导入单词");

        // --- 词库管理 ---
        var wordListsGroup = vocabularyPage.CreateChildPermission(VocabularyPermissions.WordLists.Default, "词库管理", "词库列表与分类管理");
        wordListsGroup.CreateChildPermission(VocabularyPermissions.WordLists.Query, "查询词库", "允许查询词库列表及其分类");

        // --- 用户状态管理 ---
        var userStatesGroup = vocabularyPage.CreateChildPermission(VocabularyPermissions.UserStates.Default, "学习状态", "用户学习进度与弱词分析");
        userStatesGroup.CreateChildPermission(VocabularyPermissions.UserStates.Query, "查询学习状态", "允许查询用户学习数据及弱词分析");
        userStatesGroup.CreateChildPermission(VocabularyPermissions.UserStates.Update, "更新学习状态", "允许记录用户学习进度");
    }
}
