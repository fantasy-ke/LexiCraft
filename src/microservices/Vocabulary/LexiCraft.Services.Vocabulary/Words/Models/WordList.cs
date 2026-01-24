using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

public class WordList(string name, string? category = null) : AuditAggregateRoot<long>
{
    /// <summary>
    /// 词库名称
    /// </summary>
    public string Name { get; private set; } = name;

    /// <summary>
    /// 分类 (考试 / 主题 / 难度)
    /// </summary>
    public string? Category { get; private set; } = category;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// 词库项
    /// </summary>
    public List<WordListItem> Items { get; private set; } = [];

    public void UpdateInfo(string name, string? category)
    {
        Name = name;
        Category = category;
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }

    public void AddWord(long wordId, int sortOrder = 0)
    {
        if (Items.All(x => x.WordId != wordId))
        {
            Items.Add(new WordListItem(Id, wordId, sortOrder));
        }
    }

    public void RemoveWord(long wordId)
    {
        var item = Items.FirstOrDefault(x => x.WordId == wordId);
        if (item != null)
        {
            Items.Remove(item);
        }
    }
}
