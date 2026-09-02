namespace ArchivioLessicale.API.Models.Errors.TypedErrors;

public static class TokensErrors
{
    public static Error Stolen(Guid tokenId) => new(
        ErrorCode: "TokensErrors.Stolen",
        ErrorDescription: $"Refresh token '{tokenId}' was compromised and revoked.");

    public static Error Expired(Guid tokenId) => new(
        ErrorCode: "TokensErrors.Expired",
        ErrorDescription:$"The refresh token '{tokenId}' has expired.");

    public static Error NotFound => new(
        ErrorCode: "TokensErrors.NotFound",
        ErrorDescription: $"The specified refresh token was not found.");
}