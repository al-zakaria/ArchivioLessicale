using ArchivioLessicale.API.Common.Extensions;
using ErrorOr;
using Wolverine;
using Wolverine.Http;

namespace ArchivioLessicale.API.Features.Auth.RefreshTokens;

public static class RefreshTokensEndpoint
{
    [WolverinePost("auth/auth/refresh-tokens")]
    public static async Task<IResult> Post(
        RefreshAuthTokensCommand command,
        IMessageBus messageBus,
        CancellationToken ct
    )
    {
        var result = await messageBus.InvokeAsync<ErrorOr<AuthResponse>>(command, ct);

        return result.ToResponse();
    }
}
