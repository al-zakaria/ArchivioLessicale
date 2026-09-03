namespace ArchivioLessicale.API.Services.Interfaces;

public interface ILinkService
{
    string GenerateEmailConfirmationLink(Guid userId, string encodedConfrimationEmailToken);
    string GeneratePendingEmailChangeLink(Guid userId, string encodedPendingEmailChangeToken);
    string GenerateCancellationEmailChangeToken(string encodedCancellationEmailChangeToken);

}
