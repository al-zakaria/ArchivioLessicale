using ArchivioLessicale.API.Models.DTOs.Auth;
using ArchivioLessicale.API.Models.DTOs.Tokens;
using ArchivioLessicale.API.Models.Entities;
using Mapster;

namespace ArchivioLessicale.API.Extensions;

public static class MapsterExtensions
{
    public static IssueAuthTokensRequest ToIssueAuthTokensRequest(
        this ApplicationUser user,
        Profile profile,
        ClientMetaData clientMetaData)
    {
        return user.Adapt<IssueAuthTokensRequest>() with
        {
            UserId = user.Id,
            NickName = profile.NickName,
            ClientMetaData =  clientMetaData
        };
    }
}