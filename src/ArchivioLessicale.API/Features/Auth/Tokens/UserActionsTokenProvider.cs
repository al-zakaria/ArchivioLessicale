using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace ArchivioLessicale.API.Features.Auth.Tokens;

public class UserActionsTokenProvider(UserManager<ApplicationUser> userManager)
{
    public async Task<string> GenerateEncodedEmailConfirmationTokenAsync(ApplicationUser user)
    {
        var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedEmailConfirmationToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(emailConfirmationToken)
        );
        
        return encodedEmailConfirmationToken;
    }
}