using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

public class Word : AuditEntity<long>
{
    /// <summary>
    /// 单词拼写
    /// </summary>
    public string Spelling { get; private set; }

    /// <summary>
    /// 音标
    /// </summary>
    public string? Phonetic { get; private set; }

    /// <summary>
    /// 发音 URL
    /// </summary>
    public string? PronunciationUrl { get; private set; }

    /// <summary>
    /// 释义 (JSON 格式存储)
    /// </summary>
    public string? Definitions { get; private set; }

    /// <summary>
    /// 例句 (JSON 格式存储)
    /// </summary>
    public string? Examples { get; private set; }

    /// <summary>
    /// 标签 (考试/主题/难度)
    /// </summary>
    public List<string> Tags { get; private set; } = [];

    private Word() 
    {
        Spelling = string.Empty;
    }

    private Word(string spelling, string? phonetic, string? pronunciationUrl, string? definitions, string? examples, List<string>? tags)
    {
        if (string.IsNullOrWhiteSpace(spelling))
            throw new ArgumentException("Spelling cannot be empty", nameof(spelling));

        Spelling = spelling;
        Phonetic = phonetic;
        PronunciationUrl = pronunciationUrl;
        Definitions = definitions;
        Examples = examples;
        Tags = tags ?? [];
    }

    public static Word Create(string spelling, string? phonetic, string? pronunciationUrl, string? definitions, string? examples, List<string>? tags)
    {
        return new Word(spelling, phonetic, pronunciationUrl, definitions, examples, tags);
    }
}
