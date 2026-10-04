using Mapster;

namespace ArchivioLessicale.API.Common.Extensions;

public static class MapsterExtensions
{
    public static IssueAuthTokensRequest ToIssueAuthTokensRequest(
        this ApplicationUser user,
        Profile profile, 
        ClientMetaData clientMetaData,
        Guid? actualRefreshTokenId = null
    )
    {
        return user.Adapt<IssueAuthTokensRequest>() with
        {
            UserId = user.Id,
            DisplayName = profile.DisplayName,
            NickName = profile.NickName,
            ClientMetaData = clientMetaData, 
            ActualRefreshTokenId = actualRefreshTokenId
        };
    }
}
