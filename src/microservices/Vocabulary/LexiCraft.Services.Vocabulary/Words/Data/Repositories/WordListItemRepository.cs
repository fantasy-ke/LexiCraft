using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Shared.Data;
using LexiCraft.Services.Vocabulary.Words.Models;

namespace LexiCraft.Services.Vocabulary.Words.Data.Repositories;

public class WordListItemRepository(VocabularyDbContext context)
    : QueryRepository<VocabularyDbContext, WordListItem>(context), IWordListItemRepository
{
}