using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

public class WordListItem : AuditEntity<long>
{
    private WordListItem()
    {
        WordListId = WordListId.Empty;
        WordId = WordId.Empty;
    }

    public WordListItem(WordListId wordListId, WordId wordId, int sortOrder = 0)
    {
        WordListId = wordListId;
        WordId = wordId;
        SortOrder = sortOrder;
    }

    public WordListId WordListId { get; private set; }
    public WordId WordId { get; private set; }

    /// <summary>
    ///     排序权重
    /// </summary>
    public int SortOrder { get; private set; }

    public void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}