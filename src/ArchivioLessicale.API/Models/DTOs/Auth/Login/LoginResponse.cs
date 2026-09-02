namespace ArchivioLessicale.API.Models.DTOs.Auth.Login;

public record LoginResponse(
    string AccessToken, 
    DateTimeOffset AccessTokenExpiresAt, 
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);