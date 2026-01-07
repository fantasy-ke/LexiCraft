using FluentValidation;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using MediatR;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Features.ClassifyError;

/// <summary>
/// 分类错误类型的命令
/// </summary>
public class ClassifyErrorCommand : IRequest<ClassifyErrorResponse>
{
    /// <summary>
    /// 用户提供的答案
    /// </summary>
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 正确答案
    /// </summary>
    public string ExpectedAnswer { get; set; } = string.Empty;
}

/// <summary>
/// 错误分类响应
/// </summary>
public class ClassifyErrorResponse
{
    /// <summary>
    /// 错误类型
    /// </summary>
    public ErrorType ErrorType { get; set; }

    /// <summary>
    /// 错误详情列表
    /// </summary>
    public List<ErrorDetail> ErrorDetails { get; set; } = new();

    /// <summary>
    /// 是否为拼写错误
    /// </summary>
    public bool IsSpellingError { get; set; }
}

/// <summary>
/// ClassifyErrorCommand 的验证器
/// </summary>
public class ClassifyErrorCommandValidator : AbstractValidator<ClassifyErrorCommand>
{
    public ClassifyErrorCommandValidator()
    {
        RuleFor(x => x.UserAnswer)
            .NotNull()
            .WithMessage("用户答案不能为空");

        RuleFor(x => x.ExpectedAnswer)
            .NotEmpty()
            .WithMessage("正确答案是必需的");
    }
}