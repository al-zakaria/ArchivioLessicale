using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Models.DTOs.Email;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailConfirmation(SendEmailRequest request, string encodedConfrimationEmailToken);
    Task SendPendingEmailChange(SendEmailRequest request, string pendingEmailChangeLink);
    Task SendEmailCancellationChange(SendEmailRequest request, string cancellationEmailChangeLink);
    Task SendResetPasswordEmail();
    Task SendIsUserWantResetPassword();
}
