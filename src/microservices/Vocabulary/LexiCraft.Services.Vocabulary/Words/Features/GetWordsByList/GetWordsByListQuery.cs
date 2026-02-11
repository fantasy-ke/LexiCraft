using BuildingBlocks.Mediator;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Words.Features.SearchWord;
using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.Words.Features.GetWordsByList;

public record GetWordsByListQuery(
    WordListId WordListId,
    int PageIndex = 1,
    int PageSize = 20,
    string? Seed = null) : IQuery<PagedWordResult>;

public record PagedWordResult(
    int Total,
    int PageIndex,
    int PageSize,
    string? Seed,
    List<WordDto> Data);

public class GetWordsByListQueryHandler(
    IWordRepository wordRepository,
    IWordListItemRepository wordListItemRepository)
    : IQueryHandler<GetWordsByListQuery, PagedWordResult>
{
    public async Task<PagedWordResult> Handle(GetWordsByListQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = wordListItemRepository.QueryNoTracking()
            .Where(x => x.WordListId == query.WordListId);

        var total = await dbQuery.CountAsync(cancellationToken);

        // 解决乱序分页问题的核心：基于 Seed 的确定性随机排序
        if (!string.IsNullOrEmpty(query.Seed))
        {
            // 在 C# 端生成稳定的种子哈希，确保跨数据库调用的稳定性
            // 使用简单的扰乱算法： (WordId ^ SeedHash) 并辅以大质数乘法
            long seedHash = 0;
            foreach (var c in query.Seed) seedHash = seedHash * 31 + c;

            // 使用位异或进行打乱。PostgreSQL 支持 bigint 的 ^ 运算符。
            // 这种方式比字符串拼接更有效，且能产生真正的乱序。
            dbQuery = dbQuery.OrderBy(x => x.WordId.Value ^ seedHash);
        }
        else
        {
            dbQuery = dbQuery.OrderBy(x => x.SortOrder);
        }

        var pagedWordIds = await dbQuery
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => x.WordId)
            .ToListAsync(cancellationToken);

        var words = await wordRepository.QueryNoTracking()
            .Where(x => pagedWordIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var wordsDict = words.ToDictionary(x => x.Id, x => x);
        var orderedWords = pagedWordIds
            .Where(id => wordsDict.ContainsKey(id))
            .Select(id => wordsDict[id])
            .Select(x => new WordDto(
                x.Id,
                x.Spelling,
                x.Phonetic,
                x.PronunciationUrl,
                x.Definitions,
                x.Examples,
                x.Tags))
            .ToList();

        return new PagedWordResult(total, query.PageIndex, query.PageSize, query.Seed, orderedWords);
    }
}