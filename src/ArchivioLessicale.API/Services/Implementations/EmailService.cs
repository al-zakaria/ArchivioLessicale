using MailKit.Net.Smtp;
using ArchivioLessicale.API.Services.Interfaces;
using MimeKit;
using ArchivioLessicale.API.Models.Options;
using MailKit.Security;
using Microsoft.Extensions.Options;
using ArchivioLessicale.API.Models.DTOs;
using Org.BouncyCastle.Ocsp;

namespace ArchivioLessicale.API.Services.Implementations;

public class EmailService(
    IEmailTemplatesService emailTemplatesService,
    IOptions<SmtpOptions> smtpOptions) : IEmailService
{

    public async Task SendEmailConfirmation(SendEmailRequest request, string confirmationLink)
    {
        var template = await emailTemplatesService.GenerateEmailConfirmationTemplate(request.RecipientName, 
            confirmationLink);

        request.Subject = template.Subject;
        request.HtmlBody = template.HtmlBody;
        request.TextBody = template.TextBody;

        await SendEmail(request);
    }

    public async Task SendPendingEmailChange(SendEmailRequest request, string pendingEmailChangeLink)
    {
        var template = await emailTemplatesService.GeneratePendingEmailChangingTemplate(request.RecipientEmail, 
            pendingEmailChangeLink);

        request.Subject = template.Subject;
        request.HtmlBody = template.HtmlBody;
        request.TextBody = template.TextBody;

        await SendEmail(request);
    }

    public async Task SendEmailCancellationChange(SendEmailRequest request, 
        string cancellationEmailChangeLink)
    {
        var template = await emailTemplatesService.GenerateEmailCancellationChangingTemplate(request.RecipientEmail, 
            request.NewRecipientEmail!, cancellationEmailChangeLink);
        
        request.Subject = template.Subject;
        request.HtmlBody = template.HtmlBody;
        request.TextBody = template.TextBody;

        await SendEmail(request);
    }

    private async Task SendEmail(SendEmailRequest request)
    {
        var emailMessage = GenerateEmail(request);

        await SendGeneratedEmail(emailMessage);
    }

    private MimeMessage GenerateEmail(SendEmailRequest request)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(smtpOptions.Value.FromName, smtpOptions.Value.FromEmail));
        message.To.Add(new MailboxAddress(request.RecipientName, request.RecipientEmail));

        message.Subject = request.Subject;

        var builder = new BodyBuilder
        {
            TextBody = request.TextBody,

            HtmlBody = request.HtmlBody
        };

        message.Body = builder.ToMessageBody();

        return message;
    }

    private async Task SendGeneratedEmail(MimeMessage message)
    {
        using var client = new SmtpClient();

        await client.ConnectAsync(smtpOptions.Value.Host, smtpOptions.Value.Port, SecureSocketOptions.Auto);
        await client.AuthenticateAsync(smtpOptions.Value.Username, smtpOptions.Value.Password);
        
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
