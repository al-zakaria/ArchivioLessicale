namespace ArchivioLessicale.API.Models.DTOs.Tokens;

public record GenerateRefreshTokenResponse(
    string RefreshToken, 
    DateTimeOffset RefreshTokenExpiresAt);