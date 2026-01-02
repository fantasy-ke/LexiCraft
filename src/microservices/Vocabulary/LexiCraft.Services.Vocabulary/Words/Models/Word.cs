using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

public class Word : AuditEntity<long>
{
    /// <summary>
    /// 单词拼写
    /// </summary>
    public string Spelling { get; set; }

    /// <summary>
    /// 音标
    /// </summary>
    public string? Phonetic { get; set; }

    /// <summary>
    /// 发音 URL
    /// </summary>
    public string? PronunciationUrl { get; set; }

    /// <summary>
    /// 释义 (JSON 格式存储)
    /// </summary>
    public string? Definitions { get; set; }

    /// <summary>
    /// 例句 (JSON 格式存储)
    /// </summary>
    public string? Examples { get; set; }

    /// <summary>
    /// 标签 (考试/主题/难度)
    /// </summary>
    public List<string> Tags { get; set; } = [];

    public Word() { }

    public Word(string spelling)
    {
        Spelling = spelling;
    }
}
