namespace ArchivioLessicale.API.Features.Email.Domain;

public interface IEmailTemplateDescriptor
{
    string TemplateName { get; }
}

public record EmailConfirmationTemplate(string UserName, string ConfirmationLink) : IEmailTemplateDescriptor
{
    public string TemplateName => EmailTemplatesNames.EmailConfirmationTemplate;
}

public static class EmailTemplatesNames
{
    public static string RootFolder = "";

    public static readonly string EmailConfirmationTemplate = $"{nameof(EmailConfirmationTemplate)}.html";
}