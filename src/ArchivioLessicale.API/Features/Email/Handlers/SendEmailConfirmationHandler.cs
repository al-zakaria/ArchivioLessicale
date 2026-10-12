using ArchivioLessicale.API.Common.Contracts;
using ArchivioLessicale.API.Features.Email.Common;
using ArchivioLessicale.API.Features.Email.Domain;

namespace ArchivioLessicale.API.Features.Email.Handlers;

public static class SendEmailConfirmationHandler
{
    public static async Task Handle(
        SendEmailConfirmationCommand command,
        EmailTemplateService emailTemplateService,
        EmailService emailService)
    {
        var email = await emailTemplateService.RenderTemplateAsync(
            new EmailConfirmationTemplate(command.DisplayName, command.ConfirmationLink));

        await emailService.SendEmail(command.Email, email);
    }
}