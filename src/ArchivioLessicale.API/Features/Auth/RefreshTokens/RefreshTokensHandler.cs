using ArchivioLessicale.API.Features.Auth.Tokens;
using ErrorOr;

namespace ArchivioLessicale.API.Features.Auth.RefreshTokens;

public static class RefreshTokensHandler
{
    public static async Task<ErrorOr<AuthResponse>> Handle(
        RefreshAuthTokensCommand command,
        TokenProvider tokenProvider,
        CancellationToken ct
    )
    {
        var newAuthTokens = await tokenProvider.RefreshAuthTokensAsync(
            command.RefreshToken, command.ClientMetaData, ct);

        return newAuthTokens;
    }
}