using System.ComponentModel.DataAnnotations;

namespace ArchivioLessicale.API.Models.Entities;

public class RefreshToken
{
    public Guid TokenId { get; init; }
    public string TokenHash { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid LinkedActualAccessTokenId { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    
    public string UserAgentIpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTimeOffset? LastSeenAt { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
