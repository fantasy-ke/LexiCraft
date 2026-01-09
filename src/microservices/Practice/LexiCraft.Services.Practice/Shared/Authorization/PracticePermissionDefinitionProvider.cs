using BuildingBlocks.Authentication.Permission;
using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Practice.Shared.Authorization;

/// <summary>
/// 练习服务权限定义提供程序
/// </summary>
public class PracticePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(PermissionDefinitionContext context)
    {
        // 获取或创建根页面权限 (与 Identity 服务对齐使用 "Pages")
        var pages = context.GetPermissionOrNull(PermissionConstant.Pages) ??
                    context.CreatePermission(PermissionConstant.Pages, "页面访问", "所有页面访问权限");

        // 创建练习服务模块权限
        var practicePage = pages.GetChildOrNull(PracticePermissions.Page) ??
                                 pages.CreateChildPermission(PracticePermissions.Page, "练习服务", "练习服务相关权限");

        // --- 练习任务管理 ---
        var tasksGroup = practicePage.CreateChildPermission(PracticePermissions.Tasks.Default, "练习任务", "练习任务创建与管理");
        tasksGroup.CreateChildPermission(PracticePermissions.Tasks.Create, "创建任务", "允许创建新的练习任务");
        tasksGroup.CreateChildPermission(PracticePermissions.Tasks.Query, "查询任务", "允许查询练习任务详情");
        tasksGroup.CreateChildPermission(PracticePermissions.Tasks.Complete, "完成任务", "允许完成练习任务");

        // --- 评估管理 ---
        var assessmentsGroup = practicePage.CreateChildPermission(PracticePermissions.Assessments.Default, "评估管理", "练习评估与记录管理");
        assessmentsGroup.CreateChildPermission(PracticePermissions.Assessments.Create, "创建评估", "允许创建练习评估记录");
        assessmentsGroup.CreateChildPermission(PracticePermissions.Assessments.Query, "查询评估", "允许查询评估记录");
        assessmentsGroup.CreateChildPermission(PracticePermissions.Assessments.Update, "更新评估", "允许更新评估记录");
        assessmentsGroup.CreateChildPermission(PracticePermissions.Assessments.Submit, "提交评估", "允许提交练习评估");
    }
}