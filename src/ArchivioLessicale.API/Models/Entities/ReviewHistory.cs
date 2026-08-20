namespace ArchivioLessicale.API.Models;

public class ReviewHistory
{
    public Guid Id { get; set; }
    public Guid UserWordId  { get; set; }
    public DateTime ReviewedAt  { get; set; }
    public int QualityScore { get; set; }
    public int CalculatedInterval { get; set;}
}