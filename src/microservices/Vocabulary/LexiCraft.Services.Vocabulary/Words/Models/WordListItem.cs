using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

public class WordListItem : AuditEntity<long>
{
    public long WordListId { get; private set; }
    public long WordId { get; private set; }

    /// <summary>
    /// 排序权重
    /// </summary>
    public int SortOrder { get; private set; }

    private WordListItem() { }

    public WordListItem(long wordListId, long wordId, int sortOrder = 0)
    {
        WordListId = wordListId;
        WordId = wordId;
        SortOrder = sortOrder;
    }

    public void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
