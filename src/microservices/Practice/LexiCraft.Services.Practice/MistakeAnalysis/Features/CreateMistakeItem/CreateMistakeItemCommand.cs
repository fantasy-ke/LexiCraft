using FluentValidation;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using MediatR;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Features.CreateMistakeItem;

/// <summary>
/// 创建错题项的命令
/// </summary>
public class CreateMistakeItemCommand : IRequest<CreateMistakeItemResponse>
{
    /// <summary>
    /// 答案记录ID
    /// </summary>
    public string AnswerRecordId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 单词ID
    /// </summary>
    public long WordId { get; set; }

    /// <summary>
    /// 单词拼写
    /// </summary>
    public string WordSpelling { get; set; } = string.Empty;

    /// <summary>
    /// 用户答案
    /// </summary>
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 错误类型
    /// </summary>
    public ErrorType ErrorType { get; set; }

    /// <summary>
    /// 错误详情
    /// </summary>
    public List<ErrorDetail> ErrorDetails { get; set; } = new();
}

/// <summary>
/// 创建错题项响应
/// </summary>
public class CreateMistakeItemResponse
{
    /// <summary>
    /// 创建的错题项
    /// </summary>
    public MistakeItem MistakeItem { get; set; } = new();

    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// CreateMistakeItemCommand 的验证器
/// </summary>
public class CreateMistakeItemCommandValidator : AbstractValidator<CreateMistakeItemCommand>
{
    public CreateMistakeItemCommandValidator()
    {
        RuleFor(x => x.AnswerRecordId)
            .NotEmpty()
            .WithMessage("答案记录ID是必需的");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("用户ID是必需的");

        RuleFor(x => x.WordId)
            .GreaterThan(0)
            .WithMessage("单词ID必须大于0");

        RuleFor(x => x.WordSpelling)
            .NotEmpty()
            .WithMessage("单词拼写是必需的");

        RuleFor(x => x.UserAnswer)
            .NotNull()
            .WithMessage("用户答案不能为空");
    }
}