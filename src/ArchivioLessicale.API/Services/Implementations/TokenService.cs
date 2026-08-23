using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ArchivioLessicale.API.Data;
using ArchivioLessicale.API.Models.Entities;
using ArchivioLessicale.API.Models.Options;
using ArchivioLessicale.API.Services.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArchivioLessicale.API.Services.Implementations;

public class TokenService(
    AuthDbContext dbContext,
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

    public async Task<string> GenerateRefreshToken(Guid userId)
    {
        var (refreshToken, rawToken) = CreateRefreshToken(userId);

        await dbContext.RefreshTokens.AddAsync(refreshToken);
        await dbContext.SaveChangesAsync();

        return rawToken;
    }

    public async Task<Result<(Guid UserId, string RawToken)>> ExchangeRefreshToken(
        string incomingRawToken)
    {
        var hashedTokenFromUser = HashRawToken(incomingRawToken);

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == hashedTokenFromUser);

        if (storedToken is null)
            return Result.Failure<(Guid, string)>("There is no refresh token for this user");

        var validationResult = await ValidateIncomingToken(storedToken);
        if (validationResult.IsFailure)
            return Result.Failure<(Guid, string)>("");

        return await RotateRefreshToken(storedToken);
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
        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(); 

        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    public async Task PurgeExpiredTokens()
    {
        var staleTokens = await dbContext.RefreshTokens
            .Where(token => 
                (token.RevokedAt != null && token.RevokedAt < options.Value.CutoffDate) ||
                (token.RevokedAt == null && token.ExpiresAt < DateTime.UtcNow))
            .ToListAsync();

        dbContext.RefreshTokens.RemoveRange(staleTokens);
        await dbContext.SaveChangesAsync();
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

        dbContext.CancellationEmailChangeTokens.Add(tokenEntity);
        await dbContext.SaveChangesAsync();

        return rawToken;
    }

    public string HashRawToken(string rawToken)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private List<Claim> GenerateAccessTokenClaims(ApplicationUser user)
    {
        return
        [
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Nickname, user.UserName!),
            new("security_stamp", user.SecurityStamp!)
        ];
    }

    private (RefreshToken RefreshTokenEntity, string RawRefreshToken) CreateRefreshToken(Guid userId)
    {
        var (rawToken, hashedRefreshToken) = GenerateCustomToken();

        var tokenEntity =  new RefreshToken
        {
            TokenId = Guid.NewGuid(),
            TokenHash = hashedRefreshToken,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        };

        return (tokenEntity, rawToken);
    }

    private (string RawToken, string HashedToken) GenerateCustomToken()
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashedToken = HashRawToken(rawToken);

        return (rawToken, hashedToken);
    }

    private async Task<Result> ValidateIncomingToken(RefreshToken incomingToken)
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

    private async Task<(Guid UserId, string RawToken)> RotateRefreshToken(RefreshToken oldToken)
    {
        var (refreshToken, rawToken) = CreateRefreshToken(oldToken.UserId);

        LinkRefreshTokens(oldToken, refreshToken);

        await dbContext.RefreshTokens.AddAsync(refreshToken);
        await dbContext.SaveChangesAsync();

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
