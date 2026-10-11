using ErrorOr;

namespace ArchivioLessicale.API.Features.Auth.Domain;

public static class ApplicationUserErrors
{
    public static readonly Error DuplicateEmail = Error.Conflict(
        code: "User.DuplicateEmail",
        description: "User with this email already exists."
    );

    public static readonly Error ApplicationUserOrProfileNotFound = Error.NotFound(
        code: "User.ApplicationUserOrProfileNotFound",
        description: "User with this ID is not found.");
}

public static class ProfileErrors
{
    public static readonly Error DuplicateNickName = Error.Conflict(
        code: "User.DuplicateNickName",
        description: "User with this nick name already exists."
    );
}

public static class RefreshTokenErrors
{
    public static readonly Error RefreshTokenDoesNotExist = Error.NotFound(
        code: "User.RefreshTokenDoesNotExist",
        description: "Invalid refresh token.");
    
    public static readonly Error RefreshTokenNotActive = Error.Forbidden(
        code: "User.RefreshTokenNotActive",
        description: "Refresh token is expired or revoked");

    public static readonly Error UserAgentDoesNotMatch = Error.Forbidden(
        code: "User.UserAgentDoesNotMatch",
        description: "UserAgent in refresh token and UserAgent in metadata do not match");
}