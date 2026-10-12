using System.Collections.Concurrent;
using ArchivioLessicale.API.Features.Email.Domain;
using ImTools;
using Scriban;
using Scriban.Runtime;

namespace ArchivioLessicale.API.Features.Email.Common;

public class EmailTemplateService(IWebHostEnvironment environment)
{
    private static readonly ConcurrentDictionary<string, Template> TemplateCache = new();

    public async Task<EmailTemplateResult> RenderTemplateAsync<TTemplate>(TTemplate templateModel) where
        TTemplate : IEmailTemplateDescriptor
    {
        var parsedTemplate = await GetOrParseTemplateAsync(templateModel.TemplateName);

        var context = new TemplateContext();
        var scriptObject = new ScriptObject();
        
        scriptObject.Import(templateModel);
        context.PushGlobal(scriptObject);
        
        var htmlBody = await parsedTemplate.RenderAsync(context);
        
        var subject = scriptObject["subject"]?.ToString()
            ?? "ArchivioLessicale Inc.";
        
        return new EmailTemplateResult(subject, htmlBody);
    }

    private async Task<Template> GetOrParseTemplateAsync(string templateName)
    {
        if (TemplateCache.TryGetValue(templateName, out var cachedTemplate))
            return cachedTemplate;

        var templatePath = Path.Combine(
            environment.ContentRootPath,
            EmailTemplatesNames.RootFolder,
            templateName
        );

        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template not found", templatePath);

        var rawHtml = await File.ReadAllTextAsync(templatePath);
        var parsedTemplate = Template.Parse(rawHtml);

        if (parsedTemplate.HasErrors)
            parsedTemplate.Messages.ToList().ForEach(Console.WriteLine);
        
        TemplateCache.TryAdd(templateName, parsedTemplate);
        
        return parsedTemplate;
    }
}