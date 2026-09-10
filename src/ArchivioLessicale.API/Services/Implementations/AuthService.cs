using ArchivioLessicale.API.Data;
using ArchivioLessicale.API.Extensions;
using ArchivioLessicale.API.Models.DTOs.Auth;
using ArchivioLessicale.API.Models.DTOs.Auth.Login;
using ArchivioLessicale.API.Models.DTOs.Auth.Register;
using ArchivioLessicale.API.Models.DTOs.Email;
using ArchivioLessicale.API.Models.Entities;
using ArchivioLessicale.API.Models.Primitives.Errors.TypedErrors;
using ArchivioLessicale.API.Services.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArchivioLessicale.API.Services.Implementations;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    ITokenService tokenService,
    IUserActionTokenService userActionTokenService,
    ILinkService linkService,
    IEmailService emailService) : IAuthService
{
    public async Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, ClientMetaData clientMetaData)
    {
        var isUserAlreadyExists = await userManager.FindByEmailAsync(request.Email);
        if (isUserAlreadyExists is not null)
            return Result.Failure<LoginResponse>(AuthErrors.UserWithThisAlreadyExists(request.Email));

        var isNickNameAlreadyTaken = await context.Profiles
            .FirstOrDefaultAsync(profile => profile.NickName == request.NickName);
        if (isNickNameAlreadyTaken is not null)
            return Result.Failure<LoginResponse>(AuthErrors.UserWithThisNickNameAlreadyExists(request.NickName));

        await using var transaction = await context.Database.BeginTransactionAsync();

        var applicationUser = ApplicationUser.Create(request.Email, request.PhoneNumber);
        
        var applicationUserCreationResult = await userManager.CreateAsync(applicationUser, request.Password);
        if (!applicationUserCreationResult.Succeeded)
            throw new Exception();

        var profile = Profile.Create(applicationUser.Id, request.NickName, request.DisplayName, request.Grade, applicationUser.CreatedAt);
        
        context.Profiles.Add(profile);
        
        await transaction.CommitAsync();

        var issueAuthTokensRequest = applicationUser.ToIssueAuthTokensRequest(profile, clientMetaData);
        var loginTokens = await tokenService.IssueAuthTokensAsync(issueAuthTokensRequest);
        
        var encodedEmailConfirmationToken =
            await userActionTokenService.GenerateEncodedEmailConfirmationTokenAsync(applicationUser);

        var sendEmailRequest = new SendEmailRequest
        {
            RecipientEmail = request.Email,
            RecipientName = request.NickName,
        };

        var emailConfirmationLink = linkService.GenerateEmailConfirmationLink(
            applicationUser.Id, encodedEmailConfirmationToken);
        
        await emailService.SendEmailConfirmation(sendEmailRequest, emailConfirmationLink);

        return loginTokens;
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request, ClientMetaData clientMetaData)
    {
        throw new NotImplementedException();
    }
}