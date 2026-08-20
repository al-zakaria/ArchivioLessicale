using ArchivioLessicale.API.Services.Interfaces;

namespace ArchivioLessicale.API.Services.Implementations;

public class LinkService : ILinkService
{
    public string GenerateEmailConfirmationLink(Guid userId, string encodedConfrimationEmailToken)
    {
        throw new NotImplementedException();
    }
}
