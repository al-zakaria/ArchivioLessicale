using ArchivioLessicale.API.Models.DTOs.Auth.Login;
using ArchivioLessicale.API.Models.DTOs.Tokens;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface ITokenService
{
    Task<LoginResponse> IssueAuthTokensAsync(IssueAuthTokensRequest request);
}
