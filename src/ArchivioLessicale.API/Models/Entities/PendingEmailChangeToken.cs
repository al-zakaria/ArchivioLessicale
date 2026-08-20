namespace ArchivioLessicale.API.Models.Entities;

public class PendingEmailChangeToken
{
    public string TokenHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string OldEmail { get; set; } = string.Empty;
    public string NewEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; }
}
