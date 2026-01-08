// 提交答案验证器
using FluentValidation;
using LexiCraft.Services.Practice.Assessments.Models;

namespace LexiCraft.Services.Practice.Assessments.Features.SubmitAnswer;

/// <summary>
/// 提交答案命令验证器
/// </summary>
public class SubmitAnswerValidator : AbstractValidator<SubmitAnswerCommand>
{
    /// <summary>
    /// 初始化提交答案验证器
    /// </summary>
    public SubmitAnswerValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("任务ID不能为空。");

        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("项目ID不能为空。");

        RuleFor(x => x.AssessmentType)
            .IsInEnum()
            .WithMessage("评估类型必须是有效的枚举值。");

        RuleFor(x => x.UserInput)
            .NotEmpty()// 语音评估可能不需要文本输入
            .WithMessage("用户输入不能为空。");
    }
}