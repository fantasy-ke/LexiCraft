// 提交答案命令
using LexiCraft.Services.Practice.Tasks.Models;
using LexiCraft.Services.Practice.Assessments.Models;
using MediatR;

namespace LexiCraft.Services.Practice.Assessments.Features.SubmitAnswer;

/// <summary>
/// 提交答案命令
/// </summary>
/// <param name="TaskId">任务ID</param>
/// <param name="ItemId">项目ID</param>
/// <param name="UserInput">用户输入（可选）</param>
/// <param name="AssessmentType">评估类型</param>
public record SubmitAnswerCommand(
    string TaskId,
    Guid ItemId,
    string? UserInput,
    AssessmentType AssessmentType
) : IRequest<AssessmentResult>;