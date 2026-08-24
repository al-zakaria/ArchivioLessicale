using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ArchivioLessicale.API.Data;
using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Models.Entities;
using ArchivioLessicale.API.Models.Options;
using ArchivioLessicale.API.Services.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArchivioLessicale.API.Services.Implementations;

public class TokenService(
    AuthDbContext context,
    IOptions<JwtOptions> options) : ITokenService
{
    public string GenerateAccessToken(ApplicationUser user)
    {
        var privateKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.SecretKey));
        var creds = new SigningCredentials(privateKey, SecurityAlgorithms.HmacSha256);

        var authClaims = GenerateAccessTokenClaims(user);

        var token = new JwtSecurityToken(
            issuer: options.Value.Issuer,
            audience: options.Value.Audience,
            expires: DateTime.UtcNow.AddMinutes(options.Value.AccessTokenExperitaionMinutes),
            claims: authClaims,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return accessToken;
    }

    public async Task<string> GenerateRefreshToken(Guid userId, ClientMetaData  clientMetaData)
    {
        var (refreshToken, rawToken) = CreateRefreshToken(userId, clientMetaData);

        await context.RefreshTokens.AddAsync(refreshToken);
        await context.SaveChangesAsync();

        return rawToken;
    }

    public async Task<Result<(Guid UserId, string RawToken)>> ExchangeRefreshToken(
        string incomingRawToken, ClientMetaData clientMetaData)
    {
        var validationRefreshTokenResult = await ValidateRefreshToken(incomingRawToken);
        if (validationRefreshTokenResult.IsFailure)
            return Result.Failure<(Guid UserId, string RawToken)>(validationRefreshTokenResult.Error);

        return await RotateRefreshToken(validationRefreshTokenResult.Value, clientMetaData);
    }

    public async Task<Result<string>> UpdateSession(string incomingRawRefreshToken, ApplicationUser user,
        ClientMetaData clientMetaData)
    {
        var validationRefreshTokenResult = await ValidateRefreshToken(incomingRawRefreshToken);
        if (validationRefreshTokenResult.IsFailure)
            return Result.Failure<string>(validationRefreshTokenResult.Error);

        var storedToken = validationRefreshTokenResult.Value;
        
        storedToken.LastSeenAt =  DateTime.UtcNow;
        storedToken.UserIpAddress = clientMetaData.UserIpAddress;
        storedToken.UserAgent = clientMetaData.UserAgent;

        await context.SaveChangesAsync();
        
        return GenerateAccessToken(user);
    }

    public async Task RevokeAllJwtTokens()
    {
        throw new NotImplementedException();
    }

    public async Task RevokeJwtToken()
    {
        throw new NotImplementedException();
    }

    public async Task RevokeAllTokens(Guid userId)
    {
        var tokens = await context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(); 

        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task PurgeExpiredTokens()
    {
        var staleTokens = await context.RefreshTokens
            .Where(token => 
                (token.RevokedAt != null && token.RevokedAt < options.Value.CutoffDate) ||
                (token.RevokedAt == null && token.ExpiresAt < DateTime.UtcNow))
            .ToListAsync();

        context.RefreshTokens.RemoveRange(staleTokens);
        await context.SaveChangesAsync();
    }
    
    public async Task<string> GenerateCancellationEmailChangeToken(Guid userId, string oldEmail, string newEmail)
    {
        var (rawToken, hashedCancellationEmailChangeToken) = GenerateCustomToken();

        var tokenEntity = new CancellationEmailChangeToken
        {
            TokenHash = hashedCancellationEmailChangeToken,
            UserId = userId,
            OldEmail = oldEmail,
            NewEmail = newEmail,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        context.CancellationEmailChangeTokens.Add(tokenEntity);
        await context.SaveChangesAsync();

        return rawToken;
    }

    public string HashRawToken(string rawToken)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private List<Claim> GenerateAccessTokenClaims(ApplicationUser user)
    {
        return
        [
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sid, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Nickname, user.UserName!),
            new Claim("security_stamp", user.SecurityStamp!)
        ];
    }

    private (RefreshToken RefreshTokenEntity, string RawRefreshToken) CreateRefreshToken(Guid userId,
        ClientMetaData clientMetaData)
    {
        var (rawToken, hashedRefreshToken) = GenerateCustomToken();

        var tokenEntity =  new RefreshToken
        {
            TokenId = Guid.NewGuid(),
            TokenHash = hashedRefreshToken,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            UserIpAddress = clientMetaData.UserIpAddress,
            UserAgent = clientMetaData.UserAgent,
            LastSeenAt = DateTime.UtcNow,
        };

        return (tokenEntity, rawToken);
    }

    private (string RawToken, string HashedToken) GenerateCustomToken()
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashedToken = HashRawToken(rawToken);

        return (rawToken, hashedToken);
    }

    private async Task<Result> ValidateStoredRefreshToken(RefreshToken incomingToken)
    {
        if (incomingToken.RevokedAt is not null)
        {
            await RevokeAllTokens(incomingToken.UserId); 
            return Result.Failure($"Refresh token with id {incomingToken.TokenId} was stollen.");
        }

        if (incomingToken.ExpiresAt < DateTime.UtcNow)
            return Result.Failure("This refresh token has expired.");

        return Result.Success();
    }

    private async Task<Result<RefreshToken>> ValidateRefreshToken(string incomingRefreshToken)
    {
        var hashedIncomingRefreshToken = HashRawToken(incomingRefreshToken);
        
        var storedToken = await context.RefreshTokens.FirstOrDefaultAsync(
            refreshToken => refreshToken.TokenHash == hashedIncomingRefreshToken);
        
        if (storedToken is null)
            return Result.Failure<RefreshToken>($"Refresh token with id {incomingRefreshToken} was not found.");
        
        var validationStoredRefreshTokenResult = await ValidateStoredRefreshToken(storedToken);
        if (validationStoredRefreshTokenResult.IsFailure)
            return Result.Failure<RefreshToken>(validationStoredRefreshTokenResult.Error);

        return storedToken;
    }

    private async Task<(Guid UserId, string RawToken)> RotateRefreshToken(RefreshToken oldToken, 
        ClientMetaData clientMetaData)
    {
        var (refreshToken, rawToken) = CreateRefreshToken(oldToken.UserId, clientMetaData);

        LinkRefreshTokens(oldToken, refreshToken);

        await context.RefreshTokens.AddAsync(refreshToken);
        await context.SaveChangesAsync();

        return (refreshToken.UserId, rawToken);
    }

    private void LinkRefreshTokens(RefreshToken oldToken, RefreshToken newToken)
    {
        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.ReplacedByTokenId = newToken.TokenId;
    }

    public Task EndOtherSessions()
    {
        throw new NotImplementedException();
    }

    public Task RevokeCancellationEmailChangeToken(Guid userId, string rawToken)
    {
        throw new NotImplementedException();
    }
}
