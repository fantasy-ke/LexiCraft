using FluentValidation;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using MediatR;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.EvaluateAnswer;

/// <summary>
/// 评估用户答案与预期答案的命令
/// </summary>
public class EvaluateAnswerCommand : IRequest<AnswerEvaluationResult>
{
    /// <summary>
    /// 用户提供的答案
    /// </summary>
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 预期的正确答案
    /// </summary>
    public string ExpectedAnswer { get; set; } = string.Empty;
}

/// <summary>
/// EvaluateAnswerCommand的验证器
/// </summary>
public class EvaluateAnswerCommandValidator : AbstractValidator<EvaluateAnswerCommand>
{
    public EvaluateAnswerCommandValidator()
    {
        RuleFor(x => x.UserAnswer)
            .NotNull()
            .WithMessage("用户答案不能为空");

        RuleFor(x => x.ExpectedAnswer)
            .NotNull()
            .WithMessage("预期答案不能为空");
    }
}