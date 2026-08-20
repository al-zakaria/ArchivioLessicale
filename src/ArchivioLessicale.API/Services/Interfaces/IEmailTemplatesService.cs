using ArchivioLessicale.API.Models.DTOs;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface IEmailTemplatesService
{
    Task<EmailTemplateResult> GenerateEmailConfirmationTemplate(string userName, string confirmationLink);
    Task<EmailTemplateResult> GeneratePendingEmailChangingTemplate(string userName, 
        string confirmationEmailChangeLink);
    Task<EmailTemplateResult> GenerateEmailCancellationChangingTemplate(string userName, 
        string newEmail, string cancellationEmailChangeLink);
}
