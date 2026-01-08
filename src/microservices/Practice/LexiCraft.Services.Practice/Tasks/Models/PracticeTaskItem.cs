namespace LexiCraft.Services.Practice.Tasks.Models;

public class PracticeTaskItem
{
    public Guid Id { get; private set; }
    public string WordId { get; private set; }
    public string SpellingSnapshot { get; private set; }
    public string PhoneticSnapshot { get; private set; }
    public string? PronunciationUrl { get; private set; }
    public string DefinitionSnapshot { get; private set; }
    public int OrderIndex { get; private set; }

    private PracticeTaskItem() { }

    public PracticeTaskItem(string wordId, string spelling, string phonetic, string? audio, string definition, int index)
    {
        Id = Guid.NewGuid();
        WordId = wordId;
        SpellingSnapshot = spelling;
        PhoneticSnapshot = phonetic;
        PronunciationUrl = audio;
        DefinitionSnapshot = definition;
        OrderIndex = index;
    }
}