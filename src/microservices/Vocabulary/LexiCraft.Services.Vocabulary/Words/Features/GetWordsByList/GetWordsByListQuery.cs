using BuildingBlocks.Mediator;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Words.Features.SearchWord;

namespace LexiCraft.Services.Vocabulary.Words.Features.GetWordsByList;

public record GetWordsByListQuery(long WordListId) : IQuery<List<WordDto>>;

public class GetWordsByListQueryHandler(IWordRepository wordRepository) 
    : IQueryHandler<GetWordsByListQuery, List<WordDto>>
{
    public async Task<List<WordDto>> Handle(GetWordsByListQuery query, CancellationToken cancellationToken)
    {
        var words = await wordRepository.GetByListIdAsync(query.WordListId);

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
