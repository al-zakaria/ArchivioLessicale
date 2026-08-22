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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArchivioLessicale.API.Services.Implementations;

public class TokenService(
    AuthDbContext dbContext,
    UserManager<ApplicationUser> userManager,
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
        var hashedTokenFromUser = HashRawRefreshToken(incomingRawToken);

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == hashedTokenFromUser);

        if (storedToken is null)
            return Result.Failure<(Guid, string)>("There is no refresh token for this user");

        var validationResult = await ValidateIncomingToken(storedToken);
        if (validationResult.IsFailure)
            return Result.Failure<(Guid, string)>("");

        return await RotateRefreshToken(storedToken);
    }

    public async Task<Result<string>> ExchangeAccessToken(ApplicationUser user, Guid TokenId)
    {
        // So i need make the previous JWT token invalid, i think i'll make it using the Id of old JWT token
        // i want push it to Redis/DB like token of the invalid JWT token and 
        // auth middleware will check all incoming jwt tokens that their id not equals to the invalid token id
        

        throw new NotImplementedException();
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
    
    public async Task<Result<string>> GenerateEmailConfirmationToken(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<string>("There is no user with such id.");

        var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailConfirmationToken));

        return encodedToken;
    }

    public async Task<Result<string>> GeneratePendingEmailChangeToken(Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GenerateCancellationEmailChangeToken(Guid userId)
    {
        throw new NotImplementedException();
    }

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
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashedRefreshToken = HashRawRefreshToken(rawToken);

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

    private string HashRawRefreshToken(string rawRefreshToken)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));

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
}
