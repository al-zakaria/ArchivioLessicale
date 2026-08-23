using ArchivioLessicale.API.Models.Entities;
using CSharpFunctionalExtensions;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    Task<string> GenerateRefreshToken(Guid userId);
    Task<string> GenerateCancellationEmailChangeToken(Guid userId, string oldEmail, string newEmail);
    Task RevokeCancellationEmailChangeToken(Guid userId, string rawToken);
    Task<Result<(Guid UserId, string RawToken)>> ExchangeRefreshToken(string incomingRawToken);
    Task EndOtherSessions();
    Task RevokeAllTokens(Guid userId);
    Task PurgeExpiredTokens();
    string HashRawToken(string rawToken);
}
