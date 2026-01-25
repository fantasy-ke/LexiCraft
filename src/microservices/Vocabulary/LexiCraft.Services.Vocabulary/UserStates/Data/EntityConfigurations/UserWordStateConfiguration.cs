using LexiCraft.Services.Vocabulary.UserStates.Models;
using LexiCraft.Services.Vocabulary.UserStates.Models.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LexiCraft.Services.Vocabulary.UserStates.Data.EntityConfigurations;

public class UserWordStateConfiguration : IEntityTypeConfiguration<UserWordState>
{
    public void Configure(EntityTypeBuilder<UserWordState> builder)
    {
        builder.ToTable("user_word_states");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasComment("用户 ID");

        builder.Property(x => x.WordId)
            .IsRequired()
            .HasComment("单词 ID");

        builder.Property(p => p.State).HasConversion(new ValueConverter<WordState, int>(
                v => (int)v,
                v => (WordState)v))
            .HasComment("掌握状态 (0:未学, 1:模糊, 2:掌握)");

        builder.Property(x => x.IsInWordBook)
            .HasDefaultValue(false)
            .HasComment("是否在生词本中");

        builder.Property(x => x.MasteryScore)
            .HasDefaultValue(0)
            .HasComment("掌握程度评分");

        builder.HasIndex(x => new { x.UserId, x.WordId })
            .IsUnique();

        builder.HasIndex(x => x.UserId);
    }
}