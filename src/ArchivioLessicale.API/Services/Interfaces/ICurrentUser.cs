namespace ArchivioLessicale.API.Services.Interfaces;

public interface ICurrentUser
{
    Guid Id { get; }
    string? Email { get; }
    string? UserName { get; }
}
