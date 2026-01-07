using FluentValidation;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using MediatR;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.SubmitAnswer;

/// <summary>
/// 提交和评估练习任务答案的命令
/// </summary>
public class SubmitAnswerCommand : IRequest<SubmitAnswerResponse>
{
    /// <summary>
    /// 正在回答的练习任务ID
    /// </summary>
    public string PracticeTaskId { get; set; } = string.Empty;

    /// <summary>
    /// 提交答案的用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户提供的答案
    /// </summary>
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 响应时间（以毫秒为单位）
    /// </summary>
    public long ResponseTimeMs { get; set; }
}

/// <summary>
/// 包含评估结果的响应
/// </summary>
public class SubmitAnswerResponse
{
    /// <summary>
    /// 创建的答案记录
    /// </summary>
    public AnswerRecord AnswerRecord { get; set; } = new();

    /// <summary>
    /// 如果答案不正确则创建的错题项
    /// </summary>
    public MistakeItem? MistakeItem { get; set; }

    /// <summary>
    /// 答案是否正确
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// 准确率分数（0.0到1.0）
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// 给用户的反馈信息
    /// </summary>
    public string Feedback { get; set; } = string.Empty;

    /// <summary>
    /// 成功指示器
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 如果提交失败则显示错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// SubmitAnswerCommand的验证器
/// </summary>
public class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
{
    public SubmitAnswerCommandValidator()
    {
        RuleFor(x => x.PracticeTaskId)
            .NotEmpty()
            .WithMessage("练习任务ID是必需的");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("用户ID是必需的");

        RuleFor(x => x.UserAnswer)
            .NotNull()
            .WithMessage("用户答案是必需的（可以为空字符串表示没有答案）");

        RuleFor(x => x.ResponseTimeMs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("响应时间必须非负");
    }
}