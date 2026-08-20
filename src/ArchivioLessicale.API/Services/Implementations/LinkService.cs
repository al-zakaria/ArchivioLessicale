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

    private string GenerateLink(string path, Dictionary<string, string?> queryParams)
    {
        var baseUrl = configuration.GetValue<string>("DeepLink:BaseAddress")?.TrimEnd('/');

        var uriBuilder = new UriBuilder($"{baseUrl}/{path.TrimStart('/')}");
        
        var finalLink = QueryHelpers.AddQueryString(uriBuilder.ToString(), queryParams);

        return finalLink;
    }       
}
