namespace ArchivioLessicale.API.Models.DTOs.Auth.Login;

public record LoginResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken);