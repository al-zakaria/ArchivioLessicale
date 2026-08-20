using ArchivioLessicale.API.Models.Entities;
using ArchivioLessicale.API.Models.DTOs;
using CSharpFunctionalExtensions;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessToken(ApplicationUser user);
    Task<string> GenerateRefreshToken(Guid userId);
    Task<Result<string>> GenerateEmailConfirmationToken(Guid userId);
    Task<Result<LoginResponse>> RefreshTokens(string rawTokenFromUser);
    Task RevokeAllTokens(Guid userId);
    Task PurgeExpiredTokens();
}
