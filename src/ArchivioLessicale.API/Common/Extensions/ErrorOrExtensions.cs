using ErrorOr;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ArchivioLessicale.API.Common.Extensions;

public static class ErrorOrExtensions
{
    public static IResult ToResponse<T>(this ErrorOr<T> errorOr)
    {
        if (errorOr.IsError)
            return MapErrorsToProblem(errorOr.Errors);

        return TypedResults.Ok(errorOr.Value);
    }

    private static ProblemHttpResult MapErrorsToProblem(List<Error> errors)
    {
        var firstError = errors[0];

        var statusCode = firstError.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Failure => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        return TypedResults.Problem(
            statusCode: statusCode,
            title: firstError.Code,
            detail: firstError.Description,
            extensions: new Dictionary<string, object?>
            {
                { "errors", errors.Select(e => new { code = e.Code, description = e.Description }) }
            }
        );
    }
}
