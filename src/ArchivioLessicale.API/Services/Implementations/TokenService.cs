using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ArchivioLessicale.API.Data;
using ArchivioLessicale.API.Models.DTOs.Tokens;
using ArchivioLessicale.API.Models.Entities;
using ArchivioLessicale.API.Models.Options;
using ArchivioLessicale.API.Services.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace ArchivioLessicale.API.Services.Implementations;

public class TokenService(
    JwtOptions options,
    ApplicationDbContext context) : ITokenService
{
    public GenerateTokenResponse GenerateAccessToken(GenerateAccessTokenRequest request)
    {
        var tokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(options.AccessTokenExpirationMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var authClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Nickname, request.NickName),
            new(JwtRegisteredClaimNames.Email, request.Email),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(authClaims),
            SigningCredentials = signingCredentials,
            Expires = tokenExpiresAt.UtcDateTime
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        return new GenerateTokenResponse(token, tokenExpiresAt);
    }

    public async Task<GenerateTokenResponse> GenerateRefreshToken(GenerateRefreshTokenRequest request)
    {
        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));
        
        var now = DateTimeOffset.UtcNow;
        var tokenExpiresAt = now.AddDays(7);

        var tokenEntity = new RefreshToken
        {
            TokenId = Guid.NewGuid(),
            TokenHash = tokenHash,
            UserId = request.UserId,
            LinkedActualAccessTokenId =  request.ActualAccessTokenId,
            CreatedAt = now,
            ExpiresAt = tokenExpiresAt,
            UserAgentIpAddress = request.ClientMetaData.UserAgentIpAddress,
            UserAgent = request.ClientMetaData.UserAgent,
            LastSeenAt = now
        };
        
        context.RefreshTokens.Add(tokenEntity);
        await context.SaveChangesAsync();
         
        return new GenerateTokenResponse(rawRefreshToken, tokenEntity.ExpiresAt);
    }
}
