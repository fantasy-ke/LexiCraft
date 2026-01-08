using LexiCraft.Services.Practice.PracticeTasks.Models;

namespace LexiCraft.Services.Practice.PracticeTasks.Services;

/// <summary>
/// 用于管理练习任务持久化和检索的服务
/// </summary>
public interface IPracticeTaskService
{
    /// <summary>
    /// 将生成的练习任务保存到数据库
    /// </summary>
    /// <param name="tasks">要保存的练习任务列表</param>
    /// <returns>保存的练习任务列表（包含分配的ID）</returns>
    Task<List<PracticeTask>> SaveTasksAsync(List<PracticeTask> tasks);

    /// <summary>
    /// 将单个练习任务保存到数据库
    /// </summary>
    /// <param name="task">要保存的练习任务</param>
    /// <returns>保存的练习任务（包含分配的ID）</returns>
    Task<PracticeTask> SaveTaskAsync(PracticeTask task);

    /// <summary>
    /// 更新练习任务的状态
    /// </summary>
    /// <param name="taskId">要更新的任务ID</param>
    /// <param name="status">新状态</param>
    /// <param name="userId">用于授权的用户ID</param>
    /// <returns>更新后的练习任务</returns>
    Task<PracticeTask> UpdateTaskStatusAsync(string taskId, PracticeTaskStatus status, Guid userId);

    /// <summary>
    /// 获取用户的练习任务（支持可选过滤）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="status">可选状态过滤器</param>
    /// <param name="fromDate">可选开始日期过滤器</param>
    /// <param name="toDate">可选结束日期过滤器</param>
    /// <returns>练习任务列表</returns>
    Task<List<PracticeTask>> GetUserTasksAsync(Guid userId, PracticeTaskStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// 分页获取用户的练习任务
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页码（从1开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="status">可选状态过滤器</param>
    /// <param name="fromDate">可选开始日期过滤器</param>
    /// <param name="toDate">可选结束日期过滤器</param>
    /// <returns>包含总数和任务的分页结果</returns>
    Task<(int total, List<PracticeTask> tasks)> GetUserTasksPagedAsync(Guid userId, int pageIndex, int pageSize, PracticeTaskStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// 根据ID获取练习任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="userId">用于授权的用户ID</param>
    /// <returns>练习任务，如果未找到则返回null</returns>
    Task<PracticeTask?> GetTaskByIdAsync(string taskId, Guid userId);

    /// <summary>
    /// 获取特定单词的练习任务
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="wordIds">单词ID列表</param>
    /// <param name="status">可选状态过滤器</param>
    /// <returns>练习任务列表</returns>
    Task<List<PracticeTask>> GetTasksByWordIdsAsync(Guid userId, List<long> wordIds, PracticeTaskStatus? status = null);

    /// <summary>
    /// 将任务标记为已完成
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="userId">用于授权的用户ID</param>
    /// <returns>更新后的练习任务</returns>
    Task<PracticeTask> CompleteTaskAsync(string taskId, Guid userId);

    /// <summary>
    /// 将任务标记为进行中
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="userId">用于授权的用户ID</param>
    /// <returns>更新后的练习任务</returns>
    Task<PracticeTask> StartTaskAsync(string taskId, Guid userId);

    /// <summary>
    /// 将任务标记为已放弃
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="userId">用于授权的用户ID</param>
    /// <returns>更新后的练习任务</returns>
    Task<PracticeTask> AbandonTaskAsync(string taskId, Guid userId);
}