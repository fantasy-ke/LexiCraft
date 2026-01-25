using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexiCraft.Services.Vocabulary.Words.Data.EntityConfigurations;

public class WordListItemConfiguration : IEntityTypeConfiguration<WordListItem>
{
    public void Configure(EntityTypeBuilder<WordListItem> builder)
    {
        builder.ToTable("word_list_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.WordListId, x.WordId })
            .IsUnique();
    }
}