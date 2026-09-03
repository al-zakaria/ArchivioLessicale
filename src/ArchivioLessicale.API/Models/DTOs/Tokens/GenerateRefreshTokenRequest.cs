using ArchivioLessicale.API.Models.DTOs.Auth;

namespace ArchivioLessicale.API.Models.DTOs.Tokens;

public record GenerateRefreshTokenRequest(
    Guid UserId,
    Guid ActualAccessTokenId,
    ClientMetaData ClientMetaData);