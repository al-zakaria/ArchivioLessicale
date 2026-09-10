using Microsoft.AspNetCore.Identity;

namespace ArchivioLessicale.API.Models.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; }

    public static ApplicationUser Create(string email, string phoneNumber)
    {
        return new ApplicationUser
        {
            Id  = Guid.NewGuid(),
            Email = email,
            UserName = email,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
