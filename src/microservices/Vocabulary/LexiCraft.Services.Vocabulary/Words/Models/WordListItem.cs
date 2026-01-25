using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

public class WordListItem : AuditEntity<long>
{
    private WordListItem()
    {
    }

    public WordListItem(long wordListId, long wordId, int sortOrder = 0)
    {
        WordListId = wordListId;
        WordId = wordId;
        SortOrder = sortOrder;
    }

    public long WordListId { get; private set; }
    public long WordId { get; private set; }

    /// <summary>
    ///     排序权重
    /// </summary>
    public int SortOrder { get; private set; }

    public void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}