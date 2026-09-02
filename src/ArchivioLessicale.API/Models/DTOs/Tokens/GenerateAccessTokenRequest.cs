namespace ArchivioLessicale.API.Models.DTOs.Tokens;

public record GenerateAccessTokenRequest(
    Guid UserId,
    string Email,
    string NickName);