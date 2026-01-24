using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexiCraft.Services.Vocabulary.Words.Data.EntityConfigurations;

public class WordListConfiguration : IEntityTypeConfiguration<WordList>
{
    public void Configure(EntityTypeBuilder<WordList> builder)
    {
        builder.ToTable("word_lists");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(64)
            .HasComment("词库名称");

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(32)
            .HasComment("分类");

        builder.Property(x => x.Description)
            .HasMaxLength(512)
            .HasComment("描述");

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.WordListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
