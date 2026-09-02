using System.ComponentModel.DataAnnotations;

namespace ArchivioLessicale.API.Models.Entities;

public class RefreshToken
{
    public Guid TokenId { get; init; }
    public string TokenHash { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    
    public string UserIpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime? LastSeenAt { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
