using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

public class WordList(string name, string? category = null) : AuditEntity<long>
{
    /// <summary>
    /// 词库名称
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// 分类 (考试 / 主题 / 难度)
    /// </summary>
    public string? Category { get; set; } = category;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    public void SetDescription(string? description)
    {
        Description = description;
    }
}
