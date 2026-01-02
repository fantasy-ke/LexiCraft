using BuildingBlocks.Mediator;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.UserStates.Models;
using LexiCraft.Services.Vocabulary.UserStates.Models.Enum;
using LexiCraft.Services.Vocabulary.Words.Features.SearchWord;
using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.UserStates.Features.GetWeakWords;

public record GetWeakWordsQuery(Guid UserId) : IQuery<List<WordDto>>;

public class GetWeakWordsQueryHandler(
    IUserWordStateRepository userWordStateRepository,
    IWordRepository wordRepository) 
    : IQueryHandler<GetWeakWordsQuery, List<WordDto>>
{
    public async Task<List<WordDto>> Handle(GetWeakWordsQuery query, CancellationToken cancellationToken)
    {
        var weakWordIds = await userWordStateRepository.QueryNoTracking<UserWordState>()
            .Where(x => x.UserId == query.UserId && (x.State == WordState.Vague || x.MasteryScore < 60))
            .Select(x => x.WordId)
            .ToListAsync(cancellationToken);

        var words = await wordRepository.QueryNoTracking<Word>()
            .Where(x => weakWordIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return words.Select(x => new WordDto(
            x.Id,
            x.Spelling,
            x.Phonetic,
            x.PronunciationUrl,
            x.Definitions,
            x.Examples,
            x.Tags)).ToList();
    }
}
