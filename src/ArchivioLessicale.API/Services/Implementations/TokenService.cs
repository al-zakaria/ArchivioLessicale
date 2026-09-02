using ArchivioLessicale.API.Models.DTOs.Auth;
using ArchivioLessicale.API.Models.DTOs.Tokens;
using ArchivioLessicale.API.Services.Interfaces;

namespace ArchivioLessicale.API.Services.Implementations;

public class TokenService : ITokenService
{
    public GenerateTokenResponse GenerateAccessToken(GenerateAccessTokenRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<GenerateTokenResponse> GenerateRefreshToken(Guid userId, ClientMetaData clientMetaData)
    {
        throw new NotImplementedException();
    }
}
