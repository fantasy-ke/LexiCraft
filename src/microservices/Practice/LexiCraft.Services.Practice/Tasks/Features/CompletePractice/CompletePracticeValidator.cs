// 完成练习验证器
using FluentValidation;

namespace LexiCraft.Services.Practice.Tasks.Features.CompletePractice;

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