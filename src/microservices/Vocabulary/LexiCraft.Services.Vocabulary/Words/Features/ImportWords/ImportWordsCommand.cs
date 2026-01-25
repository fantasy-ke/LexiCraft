using BuildingBlocks.Domain;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Shared.Data;
using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.Words.Features.ImportWords;

public record WordImportDto(
    string Spelling,
    string? Phonetic,
    string? PronunciationUrl,
    string? Definitions,
    string? Examples,
    List<string>? Tags);

public record ImportWordsRequest(
    string Name,
    string? Category,
    string? Description,
    List<WordImportDto> Words);

public record WordImportResult(
    long WordListId,
    int TotalProcessed,
    int NewWordsCount,
    int ExistingWordsCount,
    List<WordDuplicateInfo> DeployedWords);

public record WordDuplicateInfo(string Spelling, long Id);

public record ImportWordsCommand(ImportWordsRequest Request) : ICommand<WordImportResult>;

public class ImportWordsCommandValidator : AbstractValidator<ImportWordsCommand>
{
    public ImportWordsCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Words).NotEmpty();
    }
}

public class ImportWordsCommandHandler(
    IUnitOfWork unitOfWork,
    IWordRepository wordRepository,
    IWordListRepository wordListRepository,
    IWordListItemRepository wordListItemRepository,
    VocabularyDbContext dbContext)
    : ICommandHandler<ImportWordsCommand, WordImportResult>
{
    public async Task<WordImportResult> Handle(ImportWordsCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        // 当配置了重试策略 (如 NpgsqlRetryingExecutionStrategy) 时，
        // 必须使用 ExecutionStrategy.ExecuteAsync 来执行包含事务的代码块。
        // 我们通过 IUnitOfWork.ExecuteAsync 封装了这一行为。
        return await unitOfWork.ExecuteAsync(async () =>
        {
            await unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. 获取或创建词库
                var wordList = await GetOrCreateWordListAsync(request, cancellationToken);

                // 2. 批量处理单词（去重与新增）
                var (deployedWords, newWordsCount) = await BatchProcessWordsAsync(request.Words, cancellationToken);

                // 3. 建立关联关系
                await LinkWordsToListAsync(wordList.Id, deployedWords, cancellationToken);

                await unitOfWork.CommitTransactionAsync();

                return new WordImportResult(
                    wordList.Id,
                    request.Words.Count,
                    newWordsCount,
                    deployedWords.Count - newWordsCount,
                    deployedWords
                );
            }
            catch (Exception)
            {
                await unitOfWork.RollbackTransactionAsync();
                throw;
            }
        });
    }

    private async Task<WordList> GetOrCreateWordListAsync(ImportWordsRequest request,
        CancellationToken cancellationToken)
    {
        var wordList = await wordListRepository.FirstOrDefaultAsync(x => x.Name == request.Name);
        if (wordList == null)
        {
            wordList = new WordList(request.Name, request.Category);
            wordList.SetDescription(request.Description);
            await wordListRepository.InsertAsync(wordList);
            await unitOfWork.SaveChangesAsync();
        }

        return wordList;
    }

    private async Task<(List<WordDuplicateInfo> DeployedWords, int NewWordsCount)> BatchProcessWordsAsync(
        List<WordImportDto> wordsData, CancellationToken cancellationToken)
    {
        var allSpellings = wordsData.Select(x => x.Spelling).Distinct().ToList();
        var existingWordsInDb = await wordRepository.QueryNoTracking()
            .Where(w => allSpellings.Contains(w.Spelling))
            .ToListAsync(cancellationToken);

        var wordMap = existingWordsInDb.ToDictionary(x => x.Spelling, x => x);
        var deployedWordsResult = new List<WordDuplicateInfo>();
        var newWordsToInsert = new List<Word>();

        foreach (var data in wordsData)
            if (wordMap.TryGetValue(data.Spelling, out var existingWord))
            {
                deployedWordsResult.Add(new WordDuplicateInfo(existingWord.Spelling, existingWord.Id));
            }
            else
            {
                var newWord = Word.Create(
                    data.Spelling,
                    data.Phonetic,
                    data.PronunciationUrl,
                    data.Definitions,
                    data.Examples,
                    data.Tags
                );
                newWordsToInsert.Add(newWord);
                wordMap[newWord.Spelling] = newWord;
            }

        if (newWordsToInsert.Count > 0)
        {
            await dbContext.Words.AddRangeAsync(newWordsToInsert, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            deployedWordsResult.AddRange(newWordsToInsert.Select(x => new WordDuplicateInfo(x.Spelling, x.Id)));
        }

        return (deployedWordsResult, newWordsToInsert.Count);
    }

    private async Task LinkWordsToListAsync(long wordListId, List<WordDuplicateInfo> deployedWords,
        CancellationToken cancellationToken)
    {
        var wordList = await wordListRepository.FirstOrDefaultAsync(x => x.Id == wordListId)
                       ?? throw new InvalidOperationException("WordList not found");

        var currentWordIds = await wordListItemRepository.QueryNoTracking()
            .Where(x => x.WordListId == wordListId)
            .Select(x => x.WordId)
            .ToListAsync(cancellationToken);

        var existingWordIds = currentWordIds.ToHashSet();
        var sortOrder = currentWordIds.Count + 1;

        foreach (var mapping in deployedWords)
            if (!existingWordIds.Contains(mapping.Id))
                wordList.AddWord(mapping.Id, sortOrder++);

        await wordListRepository.UpdateAsync(wordList);
        await wordListRepository.SaveChangesAsync();
    }
}