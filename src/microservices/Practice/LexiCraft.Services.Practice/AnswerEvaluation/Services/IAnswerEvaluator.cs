using LexiCraft.Services.Practice.AnswerEvaluation.Models;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Services;

/// <summary>
/// 用于评估用户答案与预期答案的服务
/// </summary>
public interface IAnswerEvaluator
{
    /// <summary>
    /// 评估用户的答案与预期答案
    /// </summary>
    /// <param name="userAnswer">用户提供的答案</param>
    /// <param name="expectedAnswer">正确答案</param>
    /// <returns>详细的评估结果，包括准确率和反馈</returns>
    Task<AnswerEvaluationResult> EvaluateAnswerAsync(string userAnswer, string expectedAnswer);

    /// <summary>
    /// 计算用户答案的准确率分数
    /// </summary>
    /// <param name="userAnswer">用户提供的答案</param>
    /// <param name="expectedAnswer">正确答案</param>
    /// <returns>准确率分数，范围从0.0到1.0</returns>
    Task<double> CalculateAccuracyAsync(string userAnswer, string expectedAnswer);

    /// <summary>
    /// 根据用户答案生成即时反馈
    /// </summary>
    /// <param name="userAnswer">用户提供的答案</param>
    /// <param name="expectedAnswer">正确答案</param>
    /// <param name="isCorrect">答案是否正确</param>
    /// <param name="accuracy">准确率分数</param>
    /// <returns>给用户的反馈信息</returns>
    Task<string> GenerateFeedbackAsync(string userAnswer, string expectedAnswer, bool isCorrect, double accuracy);
}