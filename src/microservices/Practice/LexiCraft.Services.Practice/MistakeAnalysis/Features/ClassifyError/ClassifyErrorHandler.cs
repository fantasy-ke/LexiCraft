using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Features.ClassifyError;

/// <summary>
/// Handler for ClassifyErrorCommand
/// </summary>
public class ClassifyErrorHandler : IRequestHandler<ClassifyErrorCommand, ClassifyErrorResponse>
{
    private readonly ILogger<ClassifyErrorHandler> _logger;

    public ClassifyErrorHandler(ILogger<ClassifyErrorHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ClassifyErrorResponse> Handle(ClassifyErrorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Classifying error for user answer: '{UserAnswer}' vs expected: '{ExpectedAnswer}'",
            request.UserAnswer, request.ExpectedAnswer);

        try
        {
            // 计算准确率
            var accuracy = CalculateAccuracy(request.UserAnswer, request.ExpectedAnswer);
            
            // 分类错误类型
            var errorType = await ClassifyErrorAsync(request.UserAnswer, request.ExpectedAnswer);
            
            // 分析错误详情
            var errorDetails = await AnalyzeErrorsAsync(request.UserAnswer, request.ExpectedAnswer);
            
            // 判断是否为拼写错误
            var isSpellingError = await IsSpellingErrorAsync(request.UserAnswer, request.ExpectedAnswer, accuracy);

            _logger.LogInformation("Error classification completed. Type: {ErrorType}, IsSpellingError: {IsSpellingError}, ErrorCount: {ErrorCount}",
                errorType, isSpellingError, errorDetails.Count);

            return new ClassifyErrorResponse
            {
                ErrorType = errorType,
                ErrorDetails = errorDetails,
                IsSpellingError = isSpellingError
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while classifying error for user answer: '{UserAnswer}'", request.UserAnswer);
            throw;
        }
    }

    /// <summary>
    /// 分类错误类型
    /// </summary>
    private async Task<ErrorType> ClassifyErrorAsync(string userAnswer, string expectedAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer))
        {
            return ErrorType.CompleteError;
        }

        var accuracy = CalculateAccuracy(userAnswer, expectedAnswer);
        
        // 如果准确率很高，认为是拼写错误
        if (accuracy >= 0.7)
        {
            return ErrorType.SpellingError;
        }

        // 否则认为是完全错误
        return await Task.FromResult(ErrorType.CompleteError);
    }

    /// <summary>
    /// 分析错误详情
    /// </summary>
    private async Task<List<ErrorDetail>> AnalyzeErrorsAsync(string userAnswer, string expectedAnswer)
    {
        var errors = new List<ErrorDetail>();
        
        if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(expectedAnswer))
        {
            return errors;
        }

        var userChars = userAnswer.ToCharArray();
        var expectedChars = expectedAnswer.ToCharArray();
        
        var maxLength = Math.Max(userChars.Length, expectedChars.Length);
        
        for (int i = 0; i < maxLength; i++)
        {
            var userChar = i < userChars.Length ? userChars[i].ToString() : "";
            var expectedChar = i < expectedChars.Length ? expectedChars[i].ToString() : "";
            
            if (userChar != expectedChar)
            {
                var category = DetermineErrorCategory(userChar, expectedChar, i, userChars.Length, expectedChars.Length);
                
                errors.Add(new ErrorDetail
                {
                    Position = i,
                    Expected = expectedChar,
                    Actual = userChar,
                    Category = category
                });
            }
        }

        return await Task.FromResult(errors);
    }

    /// <summary>
    /// 判断是否为拼写错误
    /// </summary>
    private async Task<bool> IsSpellingErrorAsync(string userAnswer, string expectedAnswer, double accuracy)
    {
        // 如果准确率高于70%，认为是拼写错误
        // 如果准确率低于70%，认为是完全错误
        return await Task.FromResult(accuracy >= 0.7);
    }

    /// <summary>
    /// 计算答案准确率
    /// </summary>
    private static double CalculateAccuracy(string userAnswer, string expectedAnswer)
    {
        if (string.IsNullOrEmpty(expectedAnswer))
            return 0.0;

        if (string.Equals(userAnswer, expectedAnswer, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        // 使用 Levenshtein 距离计算相似度
        var distance = LevenshteinDistance(userAnswer ?? string.Empty, expectedAnswer);
        var maxLength = Math.Max(userAnswer?.Length ?? 0, expectedAnswer.Length);
        
        return maxLength == 0 ? 0.0 : 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// 计算 Levenshtein 距离
    /// </summary>
    private static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;
        var matrix = new int[sourceLength + 1, targetLength + 1];

        // 初始化第一行和第一列
        for (var i = 0; i <= sourceLength; i++)
            matrix[i, 0] = i;

        for (var j = 0; j <= targetLength; j++)
            matrix[0, j] = j;

        // 填充矩阵
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[sourceLength, targetLength];
    }

    /// <summary>
    /// 确定错误类别
    /// </summary>
    private static ErrorCategory DetermineErrorCategory(string actual, string expected, int position, int actualLength, int expectedLength)
    {
        if (string.IsNullOrEmpty(expected) && !string.IsNullOrEmpty(actual))
        {
            return ErrorCategory.CharacterInsertion;
        }

        if (!string.IsNullOrEmpty(expected) && string.IsNullOrEmpty(actual))
        {
            return ErrorCategory.CharacterDeletion;
        }

        if (!string.IsNullOrEmpty(expected) && !string.IsNullOrEmpty(actual))
        {
            return ErrorCategory.CharacterSubstitution;
        }

        return ErrorCategory.CharacterSubstitution;
    }
}