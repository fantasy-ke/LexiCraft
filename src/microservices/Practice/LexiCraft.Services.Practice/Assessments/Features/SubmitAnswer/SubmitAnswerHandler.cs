using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Tasks.Models;
using MediatR;

namespace LexiCraft.Services.Practice.Assessments.Features.SubmitAnswer;

/// <summary>
/// 提交答案命令处理器
/// 处理用户提交的答案并返回评估结果
/// </summary>
/// <param name="repository">练习任务仓库</param>
public class SubmitAnswerHandler(IPracticeTaskRepository repository) 
    : IRequestHandler<SubmitAnswerCommand, AssessmentResult>
{
    /// <summary>
    /// 处理提交答案命令
    /// </summary>
    /// <param name="request">提交答案命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评估结果</returns>
    public async Task<AssessmentResult> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        // 从仓库获取练习任务
        var task = await repository.FirstOrDefaultAsync(x => x.Id == request.TaskId);

        // 验证任务是否存在
        if (task == null)
        {
            throw new ArgumentException($"找不到ID为 {request.TaskId} 的练习任务。");
        }

        // 提交答案并获取评估结果
        // 调用聚合根的业务方法进行答案评估
        var result = task.SubmitAnswer(request.ItemId, request.UserInput, request.AssessmentType);

        // 更新持久化存储
        // 对于MongoDB中的聚合根模式，替换整个文档是最简单的更新方式
        await repository.UpdateAsync(task);

        return result;
    }
}