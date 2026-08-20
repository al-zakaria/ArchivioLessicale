using ArchivioLessicale.API.Models.Constants;
using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Services.Interfaces;
using Scriban;
using Scriban.Runtime;

namespace ArchivioLessicale.API.Services.Implementations;

public class EmailTemplatesService(IWebHostEnvironment environment) : IEmailTemplatesService
{
    public async Task<EmailTemplateResult> GenerateEmailConfirmationTemplate(string userName, 
        string confirmationLink)
    {
        var templateName = EmailTemplatesNames.EmailConfirmationTemplate;

        return await ParseTemplate(templateName, new
        {
            UserName = userName,
            ConfirmationLink = confirmationLink
        });
    }

    public async Task<EmailTemplateResult> GeneratePendingEmailChangingTemplate(string userName, 
        string confirmationEmailChangeLink)
    {
        var pendingEmailChangeTemplate = EmailTemplatesNames.PendingEmailChangeTemplate;

        return await ParseTemplate(pendingEmailChangeTemplate, new
        {
            UserName = userName,
            ConfirmationEmailChangeLink = confirmationEmailChangeLink
        });
    }

    public async Task<EmailTemplateResult> GenerateEmailCancellationChangingTemplate(string userName, 
        string newEmail, string cancellationEmailChangeLink)
    {
        var cancellationEmailChangeTemplate = EmailTemplatesNames.CancellationEmailChangeTemplate;

        return await ParseTemplate(cancellationEmailChangeTemplate, new
        {
            UserName = userName,
            NewEmail = newEmail,
            CancellationLink = cancellationEmailChangeLink
        });
    }

    private async Task<EmailTemplateResult> ParseTemplate(string templateName, object scriptObjectImport)
    {
        var template = await GetTemplate(templateName);
        var parsedTemplate = Template.Parse(template);

        var context = new TemplateContext();
        var scriptObject = new ScriptObject();

        scriptObject.Import(scriptObjectImport);

        context.PushGlobal(scriptObject);

        var htmlBody = await parsedTemplate.RenderAsync(context);

        var textBody = Html2Text.HtmlHelper.ToPlainText(htmlBody);

        var subject = scriptObject["subject"]?.ToString() ?? string.Empty;

        return new EmailTemplateResult(subject, htmlBody, textBody);
    }

    private async Task<string> GetTemplate(string templateName)
    {
        var templateRootFolder = EmailTemplatesNames.EmailTemplatesRootFolder;

        var templatePath = Path.Combine(environment.ContentRootPath, templateRootFolder, templateName);

        if (!File.Exists(templatePath))
            throw new Exception();

        var template = await File.ReadAllTextAsync(templatePath);

        return template;
    }
}
