// 创建练习任务验证器
using FluentValidation;

namespace LexiCraft.Services.Practice.Tasks.Features.CreatePracticeTask;

/// <summary>
/// 创建练习任务命令验证器
/// </summary>
public class CreatePracticeTaskValidator : AbstractValidator<CreatePracticeTaskCommand>
{
    /// <summary>
    /// 初始化创建练习任务验证器
    /// </summary>
    public CreatePracticeTaskValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("用户ID不能为空。");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("无效的练习任务类型。");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("无效的练习任务来源。");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("分类不能为空。")
            .MaximumLength(100)
            .WithMessage("分类名称长度不能超过100个字符。");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("练习任务项不能为空。")
            .Must(x => x.Count >= 1)
            .WithMessage("练习任务项至少需要包含一个项目。")
            .Must(x => x.Count <= 100)
            .WithMessage("练习任务项最多只能包含100个项目。");

        RuleFor(x => x.Items)
            .Must(items => items.Select(x => x.Index).Distinct().Count() == items.Count)
            .WithMessage("练习任务项的索引必须唯一。");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.WordId)
                    .NotEmpty()
                    .WithMessage("单词ID不能为空。");

                item.RuleFor(x => x.Spelling)
                    .NotEmpty()
                    .WithMessage("拼写不能为空。")
                    .MaximumLength(100)
                    .WithMessage("拼写长度不能超过100个字符。");

                item.RuleFor(x => x.Definition)
                    .NotEmpty()
                    .WithMessage("定义不能为空。");

                item.RuleFor(x => x.Index)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("索引必须大于或等于0。");
            });
    }
}