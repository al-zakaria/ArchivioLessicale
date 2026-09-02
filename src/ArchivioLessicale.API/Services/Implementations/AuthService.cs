using ArchivioLessicale.API.Data;
using ArchivioLessicale.API.Models;
using ArchivioLessicale.API.Models.Entities;
using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using CSharpFunctionalExtensions;
using System.Text;
using ArchivioLessicale.API.Models.DTOs.Auth;
using ArchivioLessicale.API.Models.DTOs.Auth.Login;
using ArchivioLessicale.API.Models.DTOs.Email;
using ArchivioLessicale.API.Models.Errors.TypedErrors;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace ArchivioLessicale.API.Services.Implementations;

public class AuthService(
    ITokenService tokenService,
    IEmailService emailService,
    ILinkService linkService,
    ICurrentUser currentUser,
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : IAuthService
{
    public async Task<Result<LoginResponse>> Register(RegisterRequest request, ClientMetaData clientMetaData)
    {
        var isUserAlreadyExists = await userManager.FindByEmailAsync(request.Email);

        if (isUserAlreadyExists is not null)
            return Result.Failure<LoginResponse>(AuthErrors.UserAlreadyExists(request.Email));
        
        await using var transaction = await context.Database.BeginTransactionAsync();

        var applicationUser = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(applicationUser, request.Password);
        if (!result.Succeeded)
            return Result.Failure<LoginResponse>($"Registration failed: {string.Join(",", result.Errors.Select(d => d.Description))}");

        var user = new Profile
        {
            Id = applicationUser.Id,
            FirstName = request.FirstName,
            SecondName = request.SecondName,
            Grade = request.Grade,
            CreatedAt = applicationUser.CreatedAt
        };

        context.Profiles.Add(user);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        var encodedConfirmationEmailToken = await GenerateEmailConfirmationToken(applicationUser);
        var confirmationLink = linkService.GenerateEmailConfirmationLink(applicationUser.Id, encodedConfirmationEmailToken.Value);
        
        var emailRequest = new SendEmailRequest
        {
            RecipientName = user.FirstName,
            RecipientEmail = applicationUser.Email
        };

        await emailService.SendEmailConfirmation(emailRequest, confirmationLink);

        var tokens = await GenerateAuthTokens(applicationUser, clientMetaData);

        return tokens;
    }

    public async Task<Result<LoginResponse>> Login(LoginRequest request, ClientMetaData clientMetaData)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result.Failure<LoginResponse>("Wrong email or password.");

        if (await userManager.IsLockedOutAsync(user))
            return Result.Failure<LoginResponse>("This user is locked out.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Result.Failure<LoginResponse>("Wrong email or password.");
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
            return Result.Failure<LoginResponse>("This email is not confirmed. Please confirm email to login.");

        await userManager.ResetAccessFailedCountAsync(user);

        var tokens = await GenerateAuthTokens(user, clientMetaData);

        return tokens;
    }

    public async Task<LoginResponse> RefreshSession(string incomingRefreshToken, ClientMetaData  clientMetaData)
    {
        var result = await tokenService.ExchangeRefreshToken(incomingRefreshToken, clientMetaData); // TODO: name this method Exchange Session for example and generate access token lì
        if (result.IsFailure)
            throw new Exception();

        var (userId, rawToken) = result.Value;

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new Exception();

        var accessToken = tokenService.GenerateAccessToken(user);

        return new LoginResponse(accessToken.Token, accessToken.TokenExpiresAt, rawToken);
    }

    public async Task<(string Token, DateTime TokenExpiresAt)> UpdateSession(string incomingRefreshToken, ClientMetaData clientMetaData)
    {
        var user = await userManager.FindByIdAsync(currentUser.Id.ToString());
        if (user is null)
            throw new Exception();
        
        var result = await tokenService.UpdateSession(incomingRefreshToken, user, clientMetaData);
        if (result.IsFailure)
            throw new Exception();
        
        return (result.Value.Token, result.Value.TokenExpiresAt);
    }

    public async Task<Result> ConfirmEmail(Guid userId, string encodedToken)
    {
        if (string.IsNullOrWhiteSpace(encodedToken))
            return Result.Failure("Token of email confirmation is null");
        
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure("There is no user with such id");

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));

        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
            return Result.Failure($"An error occurred during email confirmation of user {user.Email} with error: {result.Errors.ToList()}");

        return Result.Success();
    }

    public async Task RequestEmailConfirmation(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new Exception();

        if (await userManager.IsEmailConfirmedAsync(user))
            throw new Exception();

        var decodedToken = await GenerateEmailConfirmationToken(user);
        var emailConfirmationLink = linkService.GenerateEmailConfirmationLink(userId, decodedToken.Value);

        var sendEmailRequest = new SendEmailRequest
        {
            RecipientName = currentUser.UserName!,
            RecipientEmail = user.Email!
        };

        await emailService.SendEmailConfirmation(sendEmailRequest, emailConfirmationLink);
    }

    public async Task<Result<LoginResponse>> ChangeEmail(Guid userId, string newEmail, string token, 
        ClientMetaData clientMetaData)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<LoginResponse>("There is no user with such id.");

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

        await userManager.ChangeEmailAsync(user, newEmail, decodedToken);
        await userManager.SetUserNameAsync(user, newEmail);
        await userManager.UpdateSecurityStampAsync(user);

        await tokenService.RevokeAllTokens(user.Id);

        var loginResponse = await GenerateAuthTokens(user, clientMetaData);

        return loginResponse;
    }

    public async Task<Result<string>> RequestEmailChange(Guid userId, string newEmail, string password)
    {
        var applicationUser = await userManager.FindByIdAsync(userId.ToString());

        if (applicationUser is null)
            return Result.Failure<string>("There is not user with such id.");

        if (!await userManager.CheckPasswordAsync(applicationUser, password))
            return Result.Failure<string>("Wrong password.");

        if (applicationUser.Email == newEmail)
            return Result.Failure<string>("The new email address cannot be the same as the old one.");

        var isEmailAlreadyExists = await userManager.FindByEmailAsync(newEmail);
        if (isEmailAlreadyExists != null)
            return Result.Failure<string>("User with this email already exists.");
    
        var pendingEmailChangeToken = await GeneratePendingEmailChangeToken(applicationUser, newEmail);
        var cancellationEmailChangeToken = await tokenService.GenerateCancellationEmailChangeToken(applicationUser.Id, applicationUser.Email!, newEmail);

        var changeEmailRequest = new ChangeEmailRequest(applicationUser, newEmail, pendingEmailChangeToken.Value, cancellationEmailChangeToken);
        await SendEmailChangeRequestNotifications(changeEmailRequest);

        return pendingEmailChangeToken;
    }

    public async Task<LoginResponse> CancelEmailChange(Guid userId, 
        string rawCancellationEmailChangeToken, string rawRefreshToken, ClientMetaData clientMetaData)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new Exception();

        var incomingHashedToken = tokenService.HashRawToken(rawCancellationEmailChangeToken);

        var token = await context.CancellationEmailChangeTokens.FirstOrDefaultAsync(
            tokenHash => tokenHash.TokenHash == incomingHashedToken &&
            tokenHash.UserId == user.Id &&
            tokenHash.ExpiresAt > DateTime.UtcNow &&
            tokenHash.RevokedAt == null);

        if (token is null)
            throw new Exception();

        await userManager.UpdateSecurityStampAsync(user);
        await emailService.SendIsUserWantResetPassword();
        await tokenService.EndOtherSessions();

        // TODO: IMPLIMENT ALL THESE CAZZO CON SendIsUserWantResetPassword, EndOtherSessions E COSÌ VIA 
        // CHE SCHIFO, PERCHÉ HO SCELTO DI DIVENTARE PROGRAMMATORE 
        // AVREI POTUTO MANGIARE LA PIZZA E LAVORARE COME CONTADINO IN SICILIA 
        
        if (await userManager.GetEmailAsync(user) != token.OldEmail)
        {
            var changeEmailToken = await userManager.GenerateChangeEmailTokenAsync(user, token.OldEmail);
            await userManager.ChangeEmailAsync(user, token.OldEmail, changeEmailToken);
        }

        await tokenService.RevokeCancellationEmailChangeToken(user.Id, rawCancellationEmailChangeToken);

        return await RefreshSession(rawRefreshToken, clientMetaData);
    }

    public async Task ResetPassword()
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAccount()
    {
        throw new NotImplementedException();
    }

    private async Task<Result<string>> GenerateEmailConfirmationToken(ApplicationUser user)
    {
        var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailConfirmationToken));

        return encodedToken;
    }


    private async Task<LoginResponse> GenerateAuthTokens(ApplicationUser user, ClientMetaData clientMetaData)
    {
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = await tokenService.GenerateRefreshToken(user.Id, clientMetaData);

        return new LoginResponse(accessToken.Token, accessToken.TokenExpiresAt, refreshToken);
    }

    private async Task SendEmailChangeRequestNotifications(ChangeEmailRequest request)
    {
        var (pendingEmailChangeRequest, emailCancellationChangeRequest) = CreateNotificationEmailRequests(request.User, request.NewEmail);  

        var pendingEmailChangeLink = linkService.GeneratePendingEmailChangeLink(request.User.Id, request.PendingEmailChangeToken);
        var cancellationEmailChangeLink = linkService.GenerateCancellationEmailChangeToken(request.CancellationEmailChangeToken);

        await emailService.SendPendingEmailChange(pendingEmailChangeRequest, pendingEmailChangeLink);
        await emailService.SendEmailCancellationChange(emailCancellationChangeRequest, cancellationEmailChangeLink);
    }

    private async Task<Result<string>> GeneratePendingEmailChangeToken(ApplicationUser user, string newEmail)
    {
        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return encodedToken;
    }

    private (SendEmailRequest PendingEmailChangeRequest, SendEmailRequest EmailCancellationChangeRequest)
        CreateNotificationEmailRequests(ApplicationUser applicationUser, string newEmail)
    {
        var pendingEmailChangeRequest = new SendEmailRequest 
        {
            RecipientName = currentUser.UserName!,
            RecipientEmail = newEmail
        };

        var emailCancellationChangeRequest = new SendEmailRequest
        {
            RecipientName = currentUser.UserName!,
            RecipientEmail = applicationUser.Email!, 
            NewRecipientEmail = newEmail
        };

        return (pendingEmailChangeRequest, emailCancellationChangeRequest);
    }

    private readonly record struct ChangeEmailRequest(
        ApplicationUser User, 
        string NewEmail, 
        string PendingEmailChangeToken, 
        string CancellationEmailChangeToken);
}
