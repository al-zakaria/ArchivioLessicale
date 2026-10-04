namespace ArchivioLessicale.API.Features.Auth; 

public record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt
);

public record ClientMetaData(
    string UserAgentIpAddress,
    string UserAgent
);