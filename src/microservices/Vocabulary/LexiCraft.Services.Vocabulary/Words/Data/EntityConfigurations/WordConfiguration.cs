using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexiCraft.Services.Vocabulary.Words.Data.EntityConfigurations;

public class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.ToTable("words");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Spelling)
            .IsRequired()
            .HasMaxLength(128)
            .HasComment("单词拼写");

        builder.Property(x => x.Phonetic)
            .HasMaxLength(128)
            .HasComment("音标");

        builder.Property(x => x.PronunciationUrl)
            .HasMaxLength(256)
            .HasComment("发音 URL");

        builder.Property(x => x.Definitions)
            .HasColumnType("jsonb")
            .HasComment("释义 (JSON)");

        builder.Property(x => x.Examples)
            .HasColumnType("jsonb")
            .HasComment("例句 (JSON)");

        builder.HasIndex(x => x.Spelling);
    }
}
