namespace ArchivioLessicale.API.Models;

public class UserWord
{
    public Guid Id  { get; set; }
    public Guid UserId { get; set; }
    public Guid WordId { get; set; }
    public string? CustomTranslation { get; set; }
    public int Repetitions { get; set; }
    public double EFactor { get; set; }
    public int Interval { get; set; }
    public DateTime NextReviewDate { get; set; }
    public DateTime LastReviewDate { get; set; }
}