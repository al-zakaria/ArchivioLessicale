using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ArchivioLessicale.API.Features.Auth.Domain;
using ArchivioLessicale.API.Features.Auth.RefreshTokens;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ArchivioLessicale.API.Features.Auth.Tokens;

public record IssueAuthTokensRequest(
    Guid UserId, 
    string Email,
    string DisplayName,
    ClientMetaData ClientMetaData
);

public class TokenProvider(
    JwtOptions options,
    ApplicationDbContext context
)
{
    public async Task<AuthResponse> IssueAuthTokensAsync(IssueAuthTokensRequest request)
    {
        var accessToken = GenerateAccessToken(request.UserId, request.Email, request.DisplayName);
        var refreshToken = await GenerateRefreshTokenAsync(request.UserId, request.ClientMetaData);

        return new AuthResponse(
            accessToken.Token, accessToken.ExpiresAt,
            refreshToken.RawToken, refreshToken.Entity.ExpiresAt
        );
    }

    public async Task<ErrorOr<AuthResponse>> RefreshAuthTokensAsync(string refreshToken, ClientMetaData clientMetaData, CancellationToken ct)
    {
        var incomingHash =  Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        var storedRefreshToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == incomingHash, ct);
        if (storedRefreshToken is null)
            return RefreshTokenErrors.RefreshTokenDoesNotExist;

        if (!storedRefreshToken.IsActive)
        {
            if (storedRefreshToken.RevokedAt is not null)
                await RevokeAllRefreshTokensAsync(storedRefreshToken.UserId, ct);
            return RefreshTokenErrors.RefreshTokenNotActive;
        }

        if (storedRefreshToken.UserAgent != clientMetaData.UserAgent)
        {
            await RevokeRefreshTokenAsync(incomingHash, ct);
            return RefreshTokenErrors.UserAgentDoesNotMatch;
        }
        
        var applicationUser = await context.Users.FirstOrDefaultAsync(u => u.Id == storedRefreshToken.UserId, ct);
        var profile = await context.Profiles.FirstOrDefaultAsync(p => p.Id == storedRefreshToken.UserId, ct);

        if (applicationUser is null || profile is null)
            return ApplicationUserErrors.ApplicationUserOrProfileNotFound;
        
        var accessToken = GenerateAccessToken(applicationUser.Id, applicationUser.Email!, profile.DisplayName);
        var newRefreshToken = await GenerateRefreshTokenAsync(applicationUser.Id, clientMetaData);
        
        storedRefreshToken.RevokedAt =  DateTimeOffset.UtcNow;
        storedRefreshToken.ReplacesByTokenId = newRefreshToken.Entity.Id;
        
        await context.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken.Token, accessToken.ExpiresAt,
            newRefreshToken.RawToken, newRefreshToken.Entity.ExpiresAt
        );
    }

    private (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(Guid userId, string email, string displayName)
    {
        var tokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(options.AccessTokenExpirationMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = GenerateAccessTokenClaims(userId, email, displayName);

        var descriptor = GenerateAccessTokenDescriptor(signingCredentials, claims, tokenExpiresAt);

        var tokenHandler = new JsonWebTokenHandler();
        var token = tokenHandler.CreateToken(descriptor);

        return (token, tokenExpiresAt);
    }

    private async Task<(string RawToken, RefreshToken Entity)> GenerateRefreshTokenAsync(Guid userId, ClientMetaData clientMetaData)
    {
        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));

        var token = GenerateRefreshTokenEntity(userId, tokenHash, clientMetaData);

        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        return (rawRefreshToken, token);
    }

    private List<Claim> GenerateAccessTokenClaims(Guid userId, string email, string displayName)
    {
        return
        [
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name, displayName)
        ];
    }

    private SecurityTokenDescriptor GenerateAccessTokenDescriptor(SigningCredentials signingCredentials, List<Claim> claims, DateTimeOffset expiresAt)
    {
        return new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = signingCredentials,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt.DateTime
        };
    }

    private RefreshToken GenerateRefreshTokenEntity(Guid userId, string tokenHash, ClientMetaData clientMetaData)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(options.RefreshTokenExpirationDays);

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            UserId = userId,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            UserAgentIpAddress = clientMetaData.UserAgentIpAddress,
            UserAgent = clientMetaData.UserAgent
        };
    }

    private async Task RevokeRefreshTokenAsync(string refreshTokenHash, CancellationToken ct)
    {
        await context.RefreshTokens
            .Where(rt => rt.TokenHash == refreshTokenHash && rt.ExpiresAt < DateTimeOffset.UtcNow &&
                         rt.RevokedAt == null)
            .ExecuteUpdateAsync(st => st
                .SetProperty(rt => rt.RevokedAt, DateTimeOffset.UtcNow), ct);
    }

    private async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        await context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ForEachAsync(rt => rt.RevokedAt = DateTimeOffset.UtcNow, ct);
    }
}