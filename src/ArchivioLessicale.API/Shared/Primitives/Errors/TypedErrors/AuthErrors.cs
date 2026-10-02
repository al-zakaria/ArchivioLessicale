namespace ArchivioLessicale.API.Models.Primitives.Errors.TypedErrors;

public static class AuthErrors
{
    public static Error UserWithThisAlreadyExists(string email) => new(
        ErrorCode: "AuthErrors.UserAlreadyExists",
        ErrorDescription: $"User  with this email '{email}' was already exists.");
    
    public static Error UserWithThisNickNameAlreadyExists(string nickName) => new(
        ErrorCode: "AuthErrors.UserWithThisNickNameAlreadyExists",
        ErrorDescription: $"User with this nickname '{nickName}' was already exists.");
    
    public static Error UserCreationProccessUnexpectedError(string error) => new(
        ErrorCode: "AuthErrors.UserCreationProccessUnexpectedError",
        ErrorDescription: $"User creation proccess unexpected error: '{error}'.");
}