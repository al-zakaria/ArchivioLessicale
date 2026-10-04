using ArchivioLessicale.API.Common.Extensions;
using ArchivioLessicale.API.Features.Auth.Tokens;
using ErrorOr;
using JasperFx.Events.Documents;
using Microsoft.AspNetCore.Identity;
using Wolverine;

namespace ArchivioLessicale.API.Features.Auth.Register;

public static class RegisterHandler
{
    public static async Task<(HandlerContinuation, ErrorOr<AuthResponse>)> Before(
        RegisterUserCommand command,
        ApplicationDbContext context,
        CancellationToken ct
    )
    {
        if (await context.Users.AnyAsync(user => user.Email == command.Email, ct))
            return (HandlerContinuation.Stop, ApplicationUserErrors.DuplicateEmail);

        if (await context.Profiles.AnyAsync(user => user.NickName == command.NickName, ct))
            return (HandlerContinuation.Stop, ProfileErrors.DuplicateNickName);

        return (HandlerContinuation.Continue, default);
    }

    public static async Task<(ErrorOr<AuthResponse>, UserRegisteredEvent?)> Handle(
        RegisterUserCommand command,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        TokenProvider tokenProvider
    )
    {
        var applicationUser = ApplicationUser.Create(command.Email);
        var creationResult = await userManager.CreateAsync(applicationUser, command.Password);

        if (!creationResult.Succeeded)
            return (creationResult.Errors.ToErrorList(), default);

        var profile = Profile.Create(applicationUser.Id, command.NickName, command.DisplayName, command.FirstName, command.LastName, command.Grade, command.NumberOfWordsStudied, command.NumberOfWordsLearned);
        context.Add(profile);

        var issueAuthTokensRequest = applicationUser.ToIssueAuthTokensRequest(profile, command.ClientMetaData);
        var authResponse = await tokenProvider.IssueAuthTokensAsync(issueAuthTokensRequest);

        return (
            authResponse,
            new UserRegisteredEvent(
                applicationUser.Id, 
                applicationUser.Email!, 
                profile.NickName, 
                profile.DisplayName
            )
        );
    }
}