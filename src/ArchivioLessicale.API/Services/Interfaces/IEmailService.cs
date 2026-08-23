using ArchivioLessicale.API.Models.DTOs;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailConfirmation(SendEmailRequest request, string encodedConfrimationEmailToken);
    Task SendPendingEmailChange(SendEmailRequest request, string pendingEmailChangeLink);
    Task SendEmailCancellationChange(SendEmailRequest request, string cancellationEmailChangeLink);
    Task SendResetPasswordEmail();
    Task SendIsUserWantResetPassword();
}
