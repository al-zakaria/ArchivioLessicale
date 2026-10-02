using ArchivioLessicale.API.Models;
using ArchivioLessicale.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArchivioLessicale.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<ApplicationUser>().ToTable("Users", "auth");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles", "auth");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "auth");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "auth");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "auth");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "auth");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "auth");
        
        builder.Entity<RefreshToken>().ToTable("RefreshTokens", "auth");
        builder.Entity<CancellationEmailChangeToken>().ToTable("CancellationEmailChangeTokens", "auth");
        
        builder.Entity<Profile>().ToTable("Profiles", "app");
    }
    
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CancellationEmailChangeToken> CancellationEmailChangeTokens => Set<CancellationEmailChangeToken>();
    
    public DbSet<Profile> Profiles => Set<Profile>();
}
