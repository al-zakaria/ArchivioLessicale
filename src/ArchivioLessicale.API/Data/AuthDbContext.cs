using ArchivioLessicale.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArchivioLessicale.API.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) 
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PendingEmailChangeToken> PendingEmailChangeTokens => Set<PendingEmailChangeToken>();
}
