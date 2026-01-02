using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

public class WordListItem : AuditEntity<long>
{
    public long WordListId { get; set; }
    public long WordId { get; set; }

    /// <summary>
    /// 排序权重
    /// </summary>
    public int SortOrder { get; set; }

    public WordListItem() { }

    public WordListItem(long wordListId, long wordId)
    {
        WordListId = wordListId;
        WordId = wordId;
    }
}
