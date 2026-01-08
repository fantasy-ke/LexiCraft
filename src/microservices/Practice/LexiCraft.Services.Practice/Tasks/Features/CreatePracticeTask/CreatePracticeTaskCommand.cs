// 创建练习任务命令
using LexiCraft.Services.Practice.Tasks.Models;
using MediatR;

namespace LexiCraft.Services.Practice.Tasks.Features.CreatePracticeTask;

/// <summary>
/// 创建练习任务命令
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Type">练习任务类型</param>
/// <param name="Source">练习任务来源</param>
/// <param name="Category">练习任务分类</param>
/// <param name="Items">练习任务项列表</param>
public record CreatePracticeTaskCommand(
    string UserId,
    PracticeTaskType Type,
    PracticeTaskSource Source,
    string Category,
    List<PracticeTaskItemDto> Items
) : IRequest<CreatePracticeTaskResult>;

/// <summary>
/// 练习任务项数据传输对象
/// </summary>
/// <param name="WordId">单词ID</param>
/// <param name="Spelling">拼写</param>
/// <param name="Phonetic">音标</param>
/// <param name="AudioUrl">音频URL（可选）</param>
/// <param name="Definition">定义</param>
/// <param name="Index">索引</param>
public record PracticeTaskItemDto(
    string WordId,
    string Spelling,
    string Phonetic,
    string? AudioUrl,
    string Definition,
    int Index
);

/// <summary>
/// 创建练习任务结果
/// </summary>
/// <param name="TaskId">任务ID</param>
public record CreatePracticeTaskResult(string TaskId);