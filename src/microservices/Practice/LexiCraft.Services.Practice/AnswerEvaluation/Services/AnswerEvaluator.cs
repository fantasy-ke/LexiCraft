using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Services;

/// <summary>
/// 用于评估用户答案与预期答案的服务
/// </summary>
public class AnswerEvaluator(ILogger<AnswerEvaluator> logger) : IAnswerEvaluator
{
    public async Task<AnswerEvaluationResult> EvaluateAnswerAsync(string userAnswer, string expectedAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer))
        {
            throw new ArgumentException("用户答案不能为空", nameof(userAnswer));
        }

        if (string.IsNullOrEmpty(expectedAnswer))
        {
            throw new ArgumentException("预期答案不能为空", nameof(expectedAnswer));
        }

        try
        {
            logger.LogDebug("评估答案: '{UserAnswer}' 与预期: '{ExpectedAnswer}'", 
                userAnswer, expectedAnswer);

            // 规范化答案以进行比较（去除空白，转换为小写）
            var normalizedUserAnswer = userAnswer.Trim().ToLowerInvariant();
            var normalizedExpectedAnswer = expectedAnswer.Trim().ToLowerInvariant();

            // 检查答案是否完全正确
            var isCorrect = normalizedUserAnswer == normalizedExpectedAnswer;

            // 计算准确率分数
            var accuracy = await CalculateAccuracyAsync(userAnswer, expectedAnswer);

            // 生成反馈
            var feedback = await GenerateFeedbackAsync(userAnswer, expectedAnswer, isCorrect, accuracy);

            var result = new AnswerEvaluationResult
            {
                IsCorrect = isCorrect,
                Accuracy = accuracy,
                Errors = new List<ErrorDetail>(), // 错误分析现在由 CQRS 处理
                Feedback = feedback
            };

            logger.LogDebug("评估完成: IsCorrect={IsCorrect}, Accuracy={Accuracy}", 
                isCorrect, accuracy);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "评估答案 '{UserAnswer}' 与 '{ExpectedAnswer}' 时出错", 
                userAnswer, expectedAnswer);
            throw;
        }
    }

    public async Task<double> CalculateAccuracyAsync(string userAnswer, string expectedAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(expectedAnswer))
        {
            return 0.0;
        }

        try
        {
            // 规范化答案以进行比较
            var normalizedUserAnswer = userAnswer.Trim().ToLowerInvariant();
            var normalizedExpectedAnswer = expectedAnswer.Trim().ToLowerInvariant();

            // 如果完全正确，返回1.0
            if (normalizedUserAnswer == normalizedExpectedAnswer)
            {
                return 1.0;
            }

            // 使用Levenshtein距离计算相似度
            var distance = CalculateLevenshteinDistance(normalizedUserAnswer, normalizedExpectedAnswer);
            var maxLength = Math.Max(normalizedUserAnswer.Length, normalizedExpectedAnswer.Length);

            // 将距离转换为准确率（0.0到1.0）
            var accuracy = maxLength == 0 ? 0.0 : Math.Max(0.0, 1.0 - (double)distance / maxLength);

            logger.LogDebug("计算准确率: {Accuracy} (距离: {Distance}, 最大长度: {MaxLength})", 
                accuracy, distance, maxLength);

            return await Task.FromResult(accuracy);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "计算 '{UserAnswer}' 与 '{ExpectedAnswer}' 的准确率时出错", 
                userAnswer, expectedAnswer);
            return 0.0;
        }
    }

    public async Task<string> GenerateFeedbackAsync(string userAnswer, string expectedAnswer, bool isCorrect, double accuracy)
    {
        try
        {
            if (isCorrect)
            {
                return await Task.FromResult("正确！做得好！");
            }

            if (accuracy >= 0.8)
            {
                return await Task.FromResult($"非常接近！正确答案是 '{expectedAnswer}'。您有一个拼写小错误。");
            }

            if (accuracy >= 0.5)
            {
                return await Task.FromResult($"很好的尝试！正确答案是 '{expectedAnswer}'。请检查拼写。");
            }

            return await Task.FromResult($"正确答案是 '{expectedAnswer}'。继续练习！");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "生成反馈时出错");
            return "答案已评估。继续练习！";
        }
    }

    /// <summary>
    /// 计算两个字符串之间的Levenshtein距离
    /// </summary>
    /// <param name="source">第一个字符串</param>
    /// <param name="target">第二个字符串</param>
    /// <returns>将一个字符串转换为另一个字符串所需的最少单字符编辑次数</returns>
    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        var sourceLength = source.Length;
        var targetLength = target.Length;
        var matrix = new int[sourceLength + 1, targetLength + 1];

        // 初始化第一列和第一行
        for (var i = 0; i <= sourceLength; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= targetLength; j++)
        {
            matrix[0, j] = j;
        }

        // 填充矩阵
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1,     // 删除
                        matrix[i, j - 1] + 1),    // 插入
                    matrix[i - 1, j - 1] + cost  // 替换
                );
            }
        }

        return matrix[sourceLength, targetLength];
    }
}