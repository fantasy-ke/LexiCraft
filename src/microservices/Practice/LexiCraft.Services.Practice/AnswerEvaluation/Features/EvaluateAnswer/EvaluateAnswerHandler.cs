using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.AnswerEvaluation.Services;
using MediatR;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.EvaluateAnswer;

/// <summary>
/// 处理答案评估命令
/// </summary>
public class EvaluateAnswerHandler(IAnswerEvaluator answerEvaluator)
    : IRequestHandler<EvaluateAnswerCommand, AnswerEvaluationResult>
{
    public async Task<AnswerEvaluationResult> Handle(EvaluateAnswerCommand request, CancellationToken cancellationToken)
    {
        // 使用现有的AnswerEvaluator服务来评估答案
        return await answerEvaluator.EvaluateAnswerAsync(request.UserAnswer, request.ExpectedAnswer);
    }
}