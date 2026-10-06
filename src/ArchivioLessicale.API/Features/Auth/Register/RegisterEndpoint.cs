using ArchivioLessicale.API.Common.Extensions;
using ErrorOr;
using Wolverine;
using Wolverine.Http;

namespace ArchivioLessicale.API.Features.Auth.Register;

public static class RegisterEndpoint
{
    [WolverinePost("api/auth/register-user")]
    public static async Task<IResult> Post(
        RegisterUserCommand command,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var result = await bus.InvokeAsync<ErrorOr<AuthResponse>>(command, ct);

        return result.ToResponse();
    }
}
