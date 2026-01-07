using FluentValidation;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using MediatR;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.GetPracticeHistory;

/// <summary>
/// 查询用户练习历史记录，支持过滤和分页
/// </summary>
public class GetPracticeHistoryQuery : IRequest<GetPracticeHistoryResponse>
{
    /// <summary>
    /// 要检索历史记录的用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 过滤的起始日期（可选）
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// 过滤的结束日期（可选）
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// 按单词ID列表进行过滤（可选）
    /// </summary>
    public List<long>? WordIds { get; set; }

    /// <summary>
    /// 分页的页面索引（从0开始）
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// 分页的页面大小
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 包含练习历史记录的响应
/// </summary>
public class GetPracticeHistoryResponse
{
    /// <summary>
    /// 答案记录列表
    /// </summary>
    public List<AnswerRecord> AnswerRecords { get; set; } = new();

    /// <summary>
    /// 匹配过滤条件的记录总数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页面索引
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 使用的页面大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 是否还有更多页面
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// 成功指示器
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 如果检索失败则显示错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// GetPracticeHistoryQuery的验证器
/// </summary>
public class GetPracticeHistoryQueryValidator : AbstractValidator<GetPracticeHistoryQuery>
{
    public GetPracticeHistoryQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("用户ID是必需的");

        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0)
            .WithMessage("页面索引必须是非负数");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("页面大小必须在1到100之间");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("开始日期必须小于或等于结束日期");

        RuleFor(x => x.WordIds)
            .Must(wordIds => wordIds == null || wordIds.All(id => id > 0))
            .WithMessage("当指定时，所有单词ID必须是正数");
    }
}