using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.Words.Features.SearchWord;

public record SearchWordQuery(string Keyword) : IQuery<List<WordDto>>;

public record WordDto(
    long Id,
    string Spelling,
    string? Phonetic,
    string? PronunciationUrl,
    string? Definitions,
    string? Examples,
    List<string> Tags);

public class SearchWordQueryValidator : AbstractValidator<SearchWordQuery>
{
    public SearchWordQueryValidator()
    {
        RuleFor(x => x.Keyword).NotEmpty().MaximumLength(100);
    }
}

public class SearchWordQueryHandler(IWordRepository wordRepository)
    : IQueryHandler<SearchWordQuery, List<WordDto>>
{
    public async Task<List<WordDto>> Handle(SearchWordQuery query, CancellationToken cancellationToken)
    {
        var words = await wordRepository.QueryNoTracking()
            .Where(x => x.Spelling.Contains(query.Keyword))
            .Take(20)
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