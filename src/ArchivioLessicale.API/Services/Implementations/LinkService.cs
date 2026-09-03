using ArchivioLessicale.API.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

namespace ArchivioLessicale.API.Services.Implementations;

public class LinkService(IConfiguration configuration) : ILinkService
{
    public string GenerateEmailConfirmationLink(Guid userId, string encodedConfrimationEmailToken)
    {
        return GenerateLink("auth/confirm-email", new Dictionary<string, string?>
        {
            { "userId", userId.ToString() },
            { "token", encodedConfrimationEmailToken  }
        });
    }

    public string GeneratePendingEmailChangeLink(Guid userId, string encodedPendingEmailChangeToken)
    {
        return GenerateLink("auth/change-email", new Dictionary<string, string?>
        {
            { "userId", userId.ToString() },
            { "token", encodedPendingEmailChangeToken }
        });
    }

    public string GenerateCancellationEmailChangeToken(string encodedCancellationEmailChangeToken)
    {
        return GenerateLink("auth/cancell-email-change", new Dictionary<string, string?>
        {
            { "token", encodedCancellationEmailChangeToken }
        });
    }

    private string GenerateLink(string path, Dictionary<string, string?> queryParams)
    {
        var baseUrl = configuration.GetValue<string>("DeepLink:BaseAddress")?.TrimEnd('/');

        var uriBuilder = new UriBuilder($"{baseUrl}/{path.TrimStart('/')}");
        
        var finalLink = QueryHelpers.AddQueryString(uriBuilder.ToString(), queryParams);

        return finalLink;
    }       
}
