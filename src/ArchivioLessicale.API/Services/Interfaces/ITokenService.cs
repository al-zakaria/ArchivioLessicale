using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Models.Entities;
using CSharpFunctionalExtensions;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    Task<string> GenerateRefreshToken(Guid userId, ClientMetaData clientMetaData);
    Task<string> GenerateCancellationEmailChangeToken(Guid userId, string oldEmail, string newEmail);
    Task RevokeCancellationEmailChangeToken(Guid userId, string rawToken);
    Task<Result<(Guid UserId, string RawToken)>> ExchangeRefreshToken(string incomingRawToken, 
        ClientMetaData clientMetaData);
    Task<Result<string>> UpdateSession(string incomingRawRefreshToken, ApplicationUser user, 
        ClientMetaData clientMetaData);
    Task EndOtherSessions();
    Task RevokeAllTokens(Guid userId);
    Task PurgeExpiredTokens();
    string HashRawToken(string rawToken);
}
