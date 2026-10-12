using ArchivioLessicale.API.Common.Contracts;
using ArchivioLessicale.API.Common.Links;
using ArchivioLessicale.API.Features.Auth.Domain;
using ArchivioLessicale.API.Features.Auth.Tokens;
using ErrorOr;
using Microsoft.AspNetCore.Identity;

namespace ArchivioLessicale.API.Features.Auth.EventsHandlers;

public static class GenerateEmailConfirmationLinkOnUserRegistered
{
    public static async Task<(Error? Result, SendEmailConfirmationCommand? Command)> Handle(
        UserRegisteredEvent notification, 
        UserManager<ApplicationUser> userManager,
        UserActionsTokenProvider userActionsTokenProvider,
        LinkService linkService,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(notification.UserId.ToString());
        if (user is null) return (ApplicationUserErrors.ApplicationUserOrProfileNotFound, default);
        
        var encodedToken = await userActionsTokenProvider.GenerateEncodedEmailConfirmationTokenAsync(user);

        var link = linkService.GenerateLink(new EmailConfirmationLink(user.Id, encodedToken));
        
        return (default, 
            new SendEmailConfirmationCommand(
                notification.Email, 
                notification.DisplayName,
                link
            )
        );
    }
}