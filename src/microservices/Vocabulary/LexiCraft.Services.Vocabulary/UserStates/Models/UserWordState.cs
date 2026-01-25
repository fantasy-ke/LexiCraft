using BuildingBlocks.Domain.Internal;
using LexiCraft.Services.Vocabulary.UserStates.Models.Enum;

namespace LexiCraft.Services.Vocabulary.UserStates.Models;

public class UserWordState : AuditEntity<long>
{
    private UserWordState()
    {
    }

    public UserWordState(Guid userId, long wordId)
    {
        UserId = userId;
        WordId = wordId;
        State = WordState.New;
        MasteryScore = 0;
        IsInWordBook = false;
    }

    public Guid UserId { get; private set; }
    public long WordId { get; private set; }

    /// <summary>
    ///     掌握状态 (未学 / 模糊 / 掌握)
    /// </summary>
    public WordState State { get; private set; }

    /// <summary>
    ///     是否加入生词本
    /// </summary>
    public bool IsInWordBook { get; private set; }

    /// <summary>
    ///     掌握分数 (0-100)
    /// </summary>
    public int MasteryScore { get; private set; }

    /// <summary>
    ///     上次复习时间
    /// </summary>
    public DateTime? LastReviewedAt { get; private set; }

    public void UpdateState(WordState state)
    {
        State = state;
        LastReviewedAt = DateTime.UtcNow;
    }

    public void UpdateScore(int score)
    {
        MasteryScore = Math.Clamp(score, 0, 100);
        LastReviewedAt = DateTime.UtcNow;
    }

    public void ToggleWordBook(bool isInBook)
    {
        IsInWordBook = isInBook;
    }

    public void MarkReviewed()
    {
        LastReviewedAt = DateTime.UtcNow;
    }
}