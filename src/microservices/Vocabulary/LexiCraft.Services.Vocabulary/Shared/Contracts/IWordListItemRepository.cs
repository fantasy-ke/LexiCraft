using BuildingBlocks.Persistence.Abstractions.Repositories;
using LexiCraft.Services.Vocabulary.Words.Models;

namespace LexiCraft.Services.Vocabulary.Shared.Contracts;

public interface IWordListItemRepository : IQueryRepository<WordListItem>
{
}