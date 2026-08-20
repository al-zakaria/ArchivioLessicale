namespace ArchivioLessicale.API.Services.Interfaces;

public interface ILinkService
{
    string GenerateEmailConfirmationLink(Guid userId, string encodedConfrimationEmailToken);
}
