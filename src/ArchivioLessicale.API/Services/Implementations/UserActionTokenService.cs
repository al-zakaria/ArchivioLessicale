using System.Text;
using ArchivioLessicale.API.Models.Entities;
using ArchivioLessicale.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace ArchivioLessicale.API.Services.Implementations;

public class UserActionTokenService(
    UserManager<ApplicationUser> userManager) : IUserActionTokenService
{
    public async Task<string> GenerateEncodedEmailConfirmationTokenAsync(ApplicationUser user)
    {
        var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedEmailConfirmationToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(emailConfirmationToken));

        return encodedEmailConfirmationToken;
    }
}