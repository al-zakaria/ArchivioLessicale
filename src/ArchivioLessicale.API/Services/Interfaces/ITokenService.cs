using ArchivioLessicale.API.Models.Entities;
using CSharpFunctionalExtensions;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    Task<string> GenerateRefreshToken(Guid userId);
    Task<Result<string>> GenerateEmailConfirmationToken(Guid userId);
    Task<string> GenerateCancellationEmailChangeToken(Guid userId);
    Task<Result<(Guid UserId, string RawToken)>> ExchangeRefreshToken(string incomingRawToken);
    Task RevokeAllTokens(Guid userId);
    Task PurgeExpiredTokens();
}
