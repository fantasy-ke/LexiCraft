using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Tasks.Models;
using MediatR;

namespace LexiCraft.Services.Practice.Tasks.Features.CreatePracticeTask;

/// <summary>
/// 创建练习任务命令处理器
/// </summary>
/// <param name="repository">练习任务仓库</param>
public class CreatePracticeTaskHandler(IPracticeTaskRepository repository) 
    : IRequestHandler<CreatePracticeTaskCommand, CreatePracticeTaskResult>
{
    /// <summary>
    /// 处理创建练习任务命令
    /// </summary>
    /// <param name="request">创建练习任务命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建练习任务结果</returns>
    public async Task<CreatePracticeTaskResult> Handle(CreatePracticeTaskCommand request, CancellationToken cancellationToken)
    {
        // 将请求中的项目映射为练习任务项列表
        var items = request.Items.Select(x => 
            new PracticeTaskItem(x.WordId, x.Spelling, x.Phonetic, x.AudioUrl, x.Definition, x.Index))
            .ToList();

        // 创建新的练习任务
        var task = PracticeTask.Create(
            request.UserId,
            request.Type,
            request.Source,
            request.Category,
            items
        );

        // 插入到数据库
        await repository.InsertAsync(task);

        // 返回包含新任务 ID 的结果
        return new CreatePracticeTaskResult(task.Id);
    }
}