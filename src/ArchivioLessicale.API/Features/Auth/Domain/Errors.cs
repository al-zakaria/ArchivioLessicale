using ErrorOr;

public static class ApplicationUserErrors
{
    public static readonly Error DuplicateEmail = Error.Conflict(
        code: "User.DuplicateEmail",
        description: "User with this email already exists."
    );
}

public class ProfileErrors
{
    public static readonly Error DuplicateNickName = Error.Conflict(
        code: "User.DuplicateNickName",
        description: "User with this nick name already exists."
    );
}