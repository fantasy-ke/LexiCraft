using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

public class WordList : AuditEntity<long>
{
    /// <summary>
    /// 词库名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 分类 (考试 / 主题 / 难度)
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    public WordList() { }

    public WordList(string name, string category)
    {
        Name = name;
        Category = category;
    }
}
