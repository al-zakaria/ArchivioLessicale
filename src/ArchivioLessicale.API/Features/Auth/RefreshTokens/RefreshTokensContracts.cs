namespace ArchivioLessicale.API.Features.Auth.RefreshTokens;

public record RefreshAuthTokensCommand(
    string RefreshToken,
    ClientMetaData ClientMetaData);
    