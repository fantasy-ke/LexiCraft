using BuildingBlocks.Authentication.Permission;
using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Practice.Shared.Authorization;

public class PracticePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(PermissionDefinitionContext context)
    {
        // 获取或创建根页面权限
        var pages = context.GetPermissionOrNull(PermissionConstant.Pages) ??
                    context.CreatePermission(PermissionConstant.Pages, "页面访问", "所有页面访问权限");

        // 创建练习服务模块权限
        var practicePage = pages.GetChildOrNull(PracticePermissions.Page) ??
                           pages.CreateChildPermission(PracticePermissions.Page, "练习服务", "练习与评估相关权限");

        // --- 练习任务管理 ---
        var tasksGroup = practicePage.CreateChildPermission(PracticePermissions.Tasks.Default, "练习任务", "练习任务生成与管理");
        tasksGroup.CreateChildPermission(PracticePermissions.Tasks.Generate, "生成练习任务", "允许为用户生成练习任务");

        // --- 答案提交管理 ---
        var answersGroup = practicePage.CreateChildPermission(PracticePermissions.Answers.Default, "答案管理", "练习答案提交与评估");
        answersGroup.CreateChildPermission(PracticePermissions.Answers.Submit, "提交练习答案", "允许提交练习任务答案");

        // --- 练习历史管理 ---
        var historyGroup = practicePage.CreateChildPermission(PracticePermissions.History.Default, "练习历史", "练习记录与历史查询");
        historyGroup.CreateChildPermission(PracticePermissions.History.Query, "查询练习历史", "允许查看练习会话历史记录");

        // --- 错误分析管理 ---
        var mistakesGroup = practicePage.CreateChildPermission(PracticePermissions.Mistakes.Default, "错误分析", "练习错误统计与分析");
        mistakesGroup.CreateChildPermission(PracticePermissions.Mistakes.Query, "查询练习错误", "允许查看练习错误和错误分析");

        // --- 性能监控管理 ---
        var performanceGroup = practicePage.CreateChildPermission(PracticePermissions.Performance.Default, "性能监控", "练习服务性能监控");
        performanceGroup.CreateChildPermission(PracticePermissions.Performance.Query, "查询性能指标", "允许查看练习服务性能指标");
    }
}