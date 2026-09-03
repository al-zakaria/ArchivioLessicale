using ArchivioLessicale.API.Models.DTOs.Tokens;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface ITokenService
{
    GenerateTokenResponse GenerateAccessToken(GenerateAccessTokenRequest request);
    Task<GenerateTokenResponse> GenerateRefreshToken(GenerateRefreshTokenRequest request);
}
