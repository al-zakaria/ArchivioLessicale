using ArchivioLessicale.API.Models.DTOs.Auth;

namespace ArchivioLessicale.API.Models.DTOs.Tokens;

public record IssueAuthTokensRequest(
    Guid UserId,
    string Email,
    string NickName,
    ClientMetaData ClientMetaData);