using BuildingBlocks.Mediator;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.Words.Features.GetWordLists;

public record GetWordListsQuery(string? Category = null) : IQuery<List<WordListDto>>;

public record WordListDto(
    WordListId Id,
    string Name,
    string? Category,
    string? Description);

public class GetWordListsQueryHandler(IWordListRepository wordListRepository)
    : IQueryHandler<GetWordListsQuery, List<WordListDto>>
{
    public async Task<List<WordListDto>> Handle(GetWordListsQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = wordListRepository.QueryNoTracking();

        if (!string.IsNullOrEmpty(query.Category)) dbQuery = dbQuery.Where(x => x.Category == query.Category);

        return await dbQuery
            .OrderByDescending(x => x.Id)
            .Select(x => new WordListDto(x.Id, x.Name, x.Category, x.Description))
            .ToListAsync(cancellationToken);
    }
}