using System.Text.Json;

namespace ArchivioLessicale.API.Models.Errors;

public record Error(string ErrorCode, string ErrorDescription)
{
    public static readonly Error UnknownError = new("UnknownError", "UnknownError");
    
    public static implicit operator string (Error error) => JsonSerializer.Serialize(error);

    public static Error FromString(string? errorString)
    {
        if (string.IsNullOrWhiteSpace(errorString))
            return new Error("General.Failure", "An unknown error occurred.");

        try
        {
            return JsonSerializer.Deserialize<Error>(errorString) 
                   ?? new Error("General.Failure", errorString);
        }
        catch
        {
            return new Error("General.Failure", errorString);
        }
    }
}