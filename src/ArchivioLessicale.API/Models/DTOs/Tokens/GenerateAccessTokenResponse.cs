namespace ArchivioLessicale.API.Models.DTOs.Tokens;

public record GenerateAccessTokenResponse(
    string AccessToken, 
    DateTimeOffset AccessTokenExpiresAt,
    Guid AccessTokenId);