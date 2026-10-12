namespace ArchivioLessicale.API.Common.Contracts;

public record SendEmailConfirmationCommand(
    string Email,
    string DisplayName,
    string ConfirmationLink);