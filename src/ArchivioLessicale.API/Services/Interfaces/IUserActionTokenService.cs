using ArchivioLessicale.API.Models.Entities;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface IUserActionTokenService
{
    Task<string> GenerateEncodedEmailConfirmationTokenAsync(ApplicationUser user);
    
}