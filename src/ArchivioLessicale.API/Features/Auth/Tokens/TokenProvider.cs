using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ArchivioLessicale.API.Features.Auth.Tokens;

public record IssueAuthTokensRequest(
    Guid UserId, 
    string Email,
    string DisplayName,
    string NickName,
    ClientMetaData ClientMetaData,
    Guid? ActualRefreshTokenId = null
);

public class TokenProvider(
    JwtOptions options,
    ApplicationDbContext context
)
{
    public async Task<AuthResponse> IssueAuthTokensAsync(IssueAuthTokensRequest request)
    {
        var accessToken = GenerateAccessToken(request.UserId, request.Email, request.DisplayName);
        var refreshToken = await GenerateRefreshTokenAsync(request.UserId, request.ClientMetaData, request.ActualRefreshTokenId);

        return new AuthResponse(
            accessToken.Token, accessToken.ExpiresAt,
            refreshToken.Token, refreshToken.ExpiresAt
        );
    }

    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(Guid userId, string email, string displayName)
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

    public async Task<(string Token, DateTimeOffset ExpiresAt)> GenerateRefreshTokenAsync(Guid userId, ClientMetaData clientMetaData, Guid? actualRefreshTokenId = null)
    {
        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));

        var token = GenerateRefreshTokenEntity(userId, tokenHash, clientMetaData, actualRefreshTokenId);

        context.RefreshTokens.Add(token.TokenEntity);
        await context.SaveChangesAsync();

        return (tokenHash, token.ExpiresAt);
    }

    private List<Claim> GenerateAccessTokenClaims(Guid userId, string email, string displayName)
    {
        return new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, displayName)
        };
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

    private (RefreshToken TokenEntity, DateTimeOffset ExpiresAt) GenerateRefreshTokenEntity(Guid userId, string tokenHash, ClientMetaData clientMetaData, Guid? actualRefreshTokenId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(options.RefreshTokenExpirationDays);

        return (new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            UserId = userId,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            UserAgentIpAddress = clientMetaData.UserAgentIpAddress,
            UserAgent = clientMetaData.UserAgent,
            ReplacesByTokenId = actualRefreshTokenId
        }, expiresAt);
    }
}