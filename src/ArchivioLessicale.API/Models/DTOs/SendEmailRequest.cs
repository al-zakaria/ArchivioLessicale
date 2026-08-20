namespace ArchivioLessicale.API.Models.DTOs;

public record SendEmailRequest
{
    public string Subject { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string RecipientEmail { get; init; } = string.Empty;
    public string? NewRecipientEmail { get; init; } = string.Empty;
}
