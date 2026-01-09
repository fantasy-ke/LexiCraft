using FluentValidation;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Tasks.Models;
using MediatR;

namespace LexiCraft.Services.Practice.Tasks.Features.CreatePracticeTask;


/// <summary>
/// 创建练习任务命令
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Type">练习任务类型</param>
/// <param name="Source">练习任务来源</param>
/// <param name="Category">练习任务分类</param>
/// <param name="Items">练习任务项列表</param>
public record CreatePracticeTaskCommand(
    string UserId,
    PracticeTaskType Type,
    PracticeTaskSource Source,
    string Category,
    List<PracticeTaskItemDto> Items
) : IRequest<CreatePracticeTaskResult>;

/// <summary>
/// 练习任务项数据传输对象
/// </summary>
/// <param name="WordId">单词ID</param>
/// <param name="Spelling">拼写</param>
/// <param name="Phonetic">音标</param>
/// <param name="AudioUrl">音频URL（可选）</param>
/// <param name="Definition">定义</param>
/// <param name="Index">索引</param>
public record PracticeTaskItemDto(
    string WordId,
    string Spelling,
    string Phonetic,
    string? AudioUrl,
    string Definition,
    int Index
);

/// <summary>
/// 创建练习任务结果
/// </summary>
/// <param name="TaskId">任务ID</param>
public record CreatePracticeTaskResult(string TaskId);


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

/// <summary>
/// 创建练习任务命令处理器
/// </summary>
/// <param name="repository">练习任务仓库</param>
public class CreatePracticeTaskHandler(IPracticeTaskRepository repository) 
    : IRequestHandler<CreatePracticeTaskCommand, CreatePracticeTaskResult>
{
    /// <summary>
    /// 处理创建练习任务命令
    /// </summary>
    /// <param name="request">创建练习任务命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建练习任务结果</returns>
    public async Task<CreatePracticeTaskResult> Handle(CreatePracticeTaskCommand request, CancellationToken cancellationToken)
    {
        // 将请求中的项目映射为练习任务项列表
        var items = request.Items.Select(x => 
            new PracticeTaskItem(x.WordId, x.Spelling, x.Phonetic, x.AudioUrl, x.Definition, x.Index))
            .ToList();

        // 创建新的练习任务
        var task = PracticeTask.Create(
            request.UserId,
            request.Type,
            request.Source,
            request.Category,
            items
        );

        // 插入到数据库
        await repository.InsertAsync(task);

        // 返回包含新任务 ID 的结果
        return new CreatePracticeTaskResult(task.Id);
    }
}