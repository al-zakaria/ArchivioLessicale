using ArchivioLessicale.API.Data;
using ArchivioLessicale.API.Models;
using ArchivioLessicale.API.Models.Entities;
using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using CSharpFunctionalExtensions;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace ArchivioLessicale.API.Services.Implementations;

public class AuthService(
    ITokenService tokenService,
    IEmailService emailService,
    ILinkService linkService,
    ICurrentUser currentUser,
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : IAuthService
{
    public async Task<Result<LoginResponse>> Register(RegisterRequest request)
    {
        var isUserAlreadyExists = await userManager.FindByEmailAsync(request.Email);

        if (isUserAlreadyExists is not null)
            return Result.Failure<LoginResponse>("User with this email already exists");

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

        var user = new User
        {
            Id = applicationUser.Id,
            FirstName = request.FirstName,
            SecondName = request.SecondName,
            Grade = request.Grade,
            CreatedAt = applicationUser.CreatedAt
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        var encodedConfirmationEmailToken = await tokenService.GenerateEmailConfirmationToken(applicationUser.Id);
        var confirmationLink = linkService.GenerateEmailConfirmationLink(applicationUser.Id, encodedConfirmationEmailToken.Value);
        
        var emailRequest = new SendEmailRequest
        {
            RecipientName = user.FirstName,
            RecipientEmail = applicationUser.Email
        };

        await emailService.SendEmailConfirmation(emailRequest, confirmationLink);

        var tokens = await GenerateAuthTokens(applicationUser);

        return tokens;
    }

    public async Task<Result<LoginResponse>> Login(LoginRequest request)
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

        var tokens = await GenerateAuthTokens(user);

        return tokens;
    }

    public async Task<LoginResponse> RefreshSession(string incomingRefreshToken)
    {
        var result = await tokenService.ExchangeRefreshToken(incomingRefreshToken);
        if (result.IsFailure)
            throw new Exception();

        var (userId, rawToken) = result.Value;

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new Exception();

        var accessToken = tokenService.GenerateAccessToken(user);

        return new LoginResponse(accessToken, rawToken);
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

    public async Task<Result<string>> RequestChangeEmail(Guid userId, string newEmail, string password)
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
        var cancellationEmailChangeToken = await tokenService.GenerateCancellationEmailChangeToken(applicationUser.Id);

        var changeEmailRequest = new ChangeEmailRequest(applicationUser, newEmail, pendingEmailChangeToken, cancellationEmailChangeToken);
        await SendEmailChangeRequestNotifications(changeEmailRequest);

        return pendingEmailChangeToken;
    }

    public async Task<Result<LoginResponse>> ChangeEmail(Guid userId, string newEmail, string token)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<LoginResponse>("There is no user with such id.");

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

        await userManager.ChangeEmailAsync(user, newEmail, decodedToken);
        await userManager.SetUserNameAsync(user, newEmail);
        await userManager.UpdateSecurityStampAsync(user);

        await tokenService.RevokeAllTokens(user.Id);

        var tokens = await GenerateAuthTokens(user);

        return tokens;
    }

    public async Task<Result> CancelEmailChange(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure("There is no user with such id.");

        await userManager.UpdateSecurityStampAsync(user);
        await tokenService.RevokeAllTokens(userId);

        return Result.Success();
    }

    public async Task ResetPassword()
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAccount()
    {
        throw new NotImplementedException();
    }

    private async Task<LoginResponse> GenerateAuthTokens(ApplicationUser user)
    {
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = await tokenService.GenerateRefreshToken(user.Id);

        return new LoginResponse(accessToken, refreshToken);
    }

    private async Task SendEmailChangeRequestNotifications(ChangeEmailRequest request)
    {
        var (pendingEmailChangeRequest, emailCancellationChangeRequest) = CreateNotificationEmailRequests(request.User, request.NewEmail);  

        var pendingEmailChangeLink = linkService.GeneratePendingEmailChangeLink(request.User.Id, request.PendingEmailChangeToken);
        var cancellationEmailChangeLink = linkService.GenerateCancellationEmailChangeToken(request.CancellationEmailChangeToken);

        await emailService.SendPendingEmailChange(pendingEmailChangeRequest, pendingEmailChangeLink);
        await emailService.SendEmailCancellationChange(emailCancellationChangeRequest, cancellationEmailChangeLink);
    }

    private async Task<string> GenerateEmailConfirmationToken(ApplicationUser applicationUser)
    {
        throw new NotImplementedException();
    }

    private async Task<string> GeneratePendingEmailChangeToken(ApplicationUser applicationUser, string newEmail)
    {
        var token = await userManager.GenerateChangeEmailTokenAsync(applicationUser, newEmail);
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
