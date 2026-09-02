namespace ArchivioLessicale.API.Models.DTOs.Tokens;

public record GenerateTokenResponse(string Token, DateTimeOffset TokenExpiresAt);