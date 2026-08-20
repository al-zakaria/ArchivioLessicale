namespace ArchivioLessicale.API.Models;

public class Word
{
    public Guid Id { get; set; }
    public string Value { get; set; } = null!;
    public string Translation { get; set; } = null!;
    public string Language { get; set; } = null!;
}