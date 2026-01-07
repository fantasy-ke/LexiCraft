using FluentValidation;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using MediatR;

namespace LexiCraft.Services.Practice.PracticeTasks.Features.GeneratePracticeTasks;

/// <summary>
/// 为用户生成练习任务的命令
/// </summary>
public class GeneratePracticeTasksCommand : IRequest<GeneratePracticeTasksResponse>
{
    /// <summary>
    /// 请求练习任务的用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 要为其生成练习任务的单词ID列表
    /// </summary>
    public List<long> WordIds { get; set; } = new();

    /// <summary>
    /// 要生成的练习类型
    /// </summary>
    public PracticeType PracticeType { get; set; }

    /// <summary>
    /// 生成任务的数量（可选，默认为所有单词）
    /// </summary>
    public int? Count { get; set; }
}

/// <summary>
/// 包含生成的练习任务的响应
/// </summary>
public class GeneratePracticeTasksResponse
{
    /// <summary>
    /// 生成的练习任务列表
    /// </summary>
    public List<PracticeTask> Tasks { get; set; } = new();

    /// <summary>
    /// 生成的任务总数
    /// </summary>
    public int TotalGenerated { get; set; }

    /// <summary>
    /// 成功指示器
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 如果生成失败则显示错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// GeneratePracticeTasksCommand的验证器
/// </summary>
public class GeneratePracticeTasksCommandValidator : AbstractValidator<GeneratePracticeTasksCommand>
{
    public GeneratePracticeTasksCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("用户ID是必需的");

        RuleFor(x => x.WordIds)
            .NotEmpty()
            .WithMessage("至少需要一个单词ID")
            .Must(wordIds => wordIds.All(id => id > 0))
            .WithMessage("所有单词ID必须是正数");

        RuleFor(x => x.PracticeType)
            .IsInEnum()
            .WithMessage("练习类型必须是有效的枚举值");

        RuleFor(x => x.Count)
            .GreaterThan(0)
            .When(x => x.Count.HasValue)
            .WithMessage("当指定时，数量必须大于0");
    }
}