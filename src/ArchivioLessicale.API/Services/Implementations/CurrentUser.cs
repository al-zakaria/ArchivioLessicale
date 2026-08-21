using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ArchivioLessicale.API.Services.Interfaces;

namespace ArchivioLessicale.API.Services.Implementations;

public class CurrentUser(IHttpContextAccessor httpContext) : ICurrentUser
{
    private ClaimsPrincipal? User = httpContext.HttpContext?.User;

    public Guid Id => Guid.Parse(User!.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    public string? Email => User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
    public string? UserName => User?.FindFirst(JwtRegisteredClaimNames.Nickname)?.Value;
}
