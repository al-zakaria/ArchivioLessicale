namespace ArchivioLessicale.API.Common.Contracts;

public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string DisplayName
);