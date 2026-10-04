using ErrorOr;
using Microsoft.AspNetCore.Identity;

public static class IdentityErrorExtension
{
    public static List<Error> ToErrorList(this IEnumerable<IdentityError> errors)
    {
        return errors
            .Select(error => Error.Validation(
                code: error.Code,
                description: error.Description
            ))
            .ToList();
    }
}