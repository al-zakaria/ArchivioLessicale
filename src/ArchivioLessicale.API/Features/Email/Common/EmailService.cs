using ArchivioLessicale.API.Features.Email.Domain;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Microsoft.IdentityModel.Tokens;
using MimeKit;

namespace ArchivioLessicale.API.Features.Email.Common;

public class EmailService(
    GmailOptions gmailOptions,
    GmailService gmailService)
{
    public async Task SendEmail(string to, EmailTemplateResult template)
    {
        try
        {
            var mimeMessage = new MimeMessage();
            
            mimeMessage.From.Add(new MailboxAddress(gmailOptions.SectionName, gmailOptions.SenderEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(to));
            mimeMessage.Subject = template.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = template.HtmlBody,
            };
            mimeMessage.Body = bodyBuilder.ToMessageBody();
            
            using var memoryStream = new MemoryStream();
            await mimeMessage.WriteToAsync(memoryStream);
            
            var rawBase64Url = Base64UrlEncoder.Encode(memoryStream.ToArray());

            var message = new Message
            {
                Raw = rawBase64Url
            };

            var request = gmailService.Users.Messages.Send(message, "me");
            await request.ExecuteAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}