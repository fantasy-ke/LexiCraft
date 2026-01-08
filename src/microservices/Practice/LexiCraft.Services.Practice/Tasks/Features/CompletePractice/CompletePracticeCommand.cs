// 完成练习命令
using MediatR;

namespace LexiCraft.Services.Practice.Tasks.Features.CompletePractice;

/// <summary>
/// 完成练习命令
/// </summary>
/// <param name="TaskId">任务ID</param>
public record CompletePracticeCommand(string TaskId) : IRequest<bool>;