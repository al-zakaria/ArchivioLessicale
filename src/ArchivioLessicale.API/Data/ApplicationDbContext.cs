using ArchivioLessicale.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ArchivioLessicale.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
}
