namespace ArchivioLessicale.API.Models;

public class Article
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string SourceUrl { get; set; } = null!;
    public string? RawText { get; set; }
    public DateTime CreatedAt { get; set; }
}