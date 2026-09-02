namespace ArchivioLessicale.API.Models.Errors.TypedErrors;

public static class AuthErrors
{
    public static Error UserAlreadyExists(string email) => new(
        ErrorCode: "AuthErrors.UserAlreadyExists",
        ErrorDescription: $"User  with this email '{email}' was already exists.");
}