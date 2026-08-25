using ArchivioLessicale.API.Models;
using ArchivioLessicale.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArchivioLessicale.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CancellationEmailChangeToken> CancellationEmailChangeTokens => Set<CancellationEmailChangeToken>();
    
    public DbSet<User> Users => Set<User>();
}
