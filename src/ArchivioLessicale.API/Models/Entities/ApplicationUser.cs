using Microsoft.AspNetCore.Identity;

namespace ArchivioLessicale.API.Models.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; }
}
