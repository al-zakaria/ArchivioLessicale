using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArchivioLessicale.API.Models.DTOs.Auth;
using ArchivioLessicale.API.Models.DTOs.Tokens;
using ArchivioLessicale.API.Models.Options;
using ArchivioLessicale.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace ArchivioLessicale.API.Services.Implementations;

public class TokenService(JwtOptions options) : ITokenService
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

    public Task<GenerateTokenResponse> GenerateRefreshToken(Guid userId, ClientMetaData clientMetaData)
    {
        throw new NotImplementedException();
    }
}
