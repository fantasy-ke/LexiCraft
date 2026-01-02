using BuildingBlocks.Domain.Internal;
using LexiCraft.Services.Vocabulary.UserStates.Models.Enum;

namespace LexiCraft.Services.Vocabulary.UserStates.Models;

public class UserWordState : AuditEntity<long>
{
    public Guid UserId { get; set; }
    public long WordId { get; set; }

    /// <summary>
    /// 掌握状态 (未学 / 模糊 / 掌握)
    /// </summary>
    public WordState State { get; set; }

    /// <summary>
    /// 是否加入生词本
    /// </summary>
    public bool IsInWordBook { get; set; }

    /// <summary>
    /// 掌握分数 (0-100)
    /// </summary>
    public int MasteryScore { get; set; }

    /// <summary>
    /// 上次复习时间
    /// </summary>
    public DateTime? LastReviewedAt { get; set; }

    public UserWordState() { }

    public UserWordState(Guid userId, long wordId)
    {
        UserId = userId;
        WordId = wordId;
        State = WordState.New;
    }
}
