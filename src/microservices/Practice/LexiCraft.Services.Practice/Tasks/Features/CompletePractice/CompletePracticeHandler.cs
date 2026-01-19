using BuildingBlocks.EventBus.Abstractions;
using FluentValidation;
using LexiCraft.Services.Practice.Assessments.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Events.IntegrationEvents;
using LexiCraft.Services.Practice.Tasks.Models;
using MediatR;

namespace LexiCraft.Services.Practice.Tasks.Features.CompletePractice;

/// <summary>
/// 完成练习命令
/// </summary>
/// <param name="TaskId">任务ID</param>
public record CompletePracticeCommand(string TaskId) : IRequest<bool>;

/// <summary>
/// 完成练习命令验证器
/// </summary>
public class CompletePracticeValidator : AbstractValidator<CompletePracticeCommand>
{
    /// <summary>
    /// 初始化完成练习验证器
    /// </summary>
    public CompletePracticeValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("任务ID不能为空。");
    }
}

/// <summary>
/// 处理完成练习任务的命令处理器
/// 负责更新任务状态、计算结果并发布相关集成事件
/// </summary>
public class CompletePracticeHandler : IRequestHandler<CompletePracticeCommand, bool>
{
    private readonly IPracticeTaskRepository _repository;
    private readonly IEventBus<IntegrationEvent> _eventBus;

    /// <summary>
    /// 构造函数，注入依赖项
    /// </summary>
    /// <param name="repository">练习任务仓库，用于数据访问</param>
    /// <param name="eventBus">事件总线，用于发布集成事件</param>
    public CompletePracticeHandler(IPracticeTaskRepository repository, IEventBus<IntegrationEvent> eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    /// <summary>
    /// 处理完成练习命令的主逻辑
    /// </summary>
    /// <param name="request">包含任务ID等信息的命令请求</param>
    /// <param name="cancellationToken">取消操作的令牌</param>
    /// <returns>异步返回布尔值，表示处理是否成功</returns>
    public async Task<bool> Handle(CompletePracticeCommand request, CancellationToken cancellationToken)
    {
        // 根据任务ID从仓库中查找对应的练习任务
        var task = await _repository.FirstOrDefaultAsync(x => x.Id == request.TaskId);

        // 如果任务不存在，抛出异常
        if (task == null) 
            throw new InvalidOperationException("Task not found");

        // 如果任务已经是完成状态，则无需重复处理
        if (task.Status == PracticeStatus.Finished) 
            return true; 

        // 调用领域方法完成任务（设置完成时间、更新状态等）
        task.Complete();

        // 将更新后的任务保存回数据库
        await _repository.UpdateAsync(task);

        // 计算正确答案数量
        var correctCount = task.Answers.Count(x => x.Status == AnswerStatus.Correct);
        // 计算错误答案数量（包括错误、部分正确和未作答）
        var wrongCount = task.Answers.Count(x => x.Status != AnswerStatus.Correct);
        // 计算练习持续时间（秒）
        var duration = (long)(task.FinishedAt!.Value - task.StartedAt!.Value).TotalSeconds;

        // 创建“练习完成”集成事件
        var completedEvent = new PracticeCompletedIntegrationEvent(
            task.UserId,
            task.Id,
            task.Items.Count,
            correctCount,
            wrongCount,
            duration,
            task.FinishedAt.Value
        );

        // 通过事件总线发布“练习完成”事件
        await _eventBus.PublishAsync(completedEvent);

        // 筛选出所有出错的答案项（错误、部分正确、未作答）
        var mistakes = task.Answers.Where(x => x.Status == AnswerStatus.Wrong || x.Status == AnswerStatus.Partial || x.Status == AnswerStatus.NoAnswer);
        foreach (var mistake in mistakes)
        {
            // 根据答案项找到对应的练习题目
            var item = task.Items.First(x => x.Id == mistake.PracticeTaskItemId);
            // 判断错误类型：未作答或拼写错误
            var mistakeType = mistake.Status == AnswerStatus.NoAnswer ? MistakeType.NoAnswer : MistakeType.SpellingError; 
            
            // 创建“单词出错”集成事件
            var mistakeEvent = new WordMistakeOccurredIntegrationEvent(
                task.UserId,
                item.WordId,
                mistakeType.ToString(),
                mistake.UserInput ?? string.Empty,
                item.SpellingSnapshot,
                mistake.SubmittedAt
            );
            // 通过事件总线发布“单词出错”事件
            await _eventBus.PublishAsync(mistakeEvent);
        }

        // 返回成功标志
        return true;
    }
}