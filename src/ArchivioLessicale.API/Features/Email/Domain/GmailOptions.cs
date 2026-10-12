namespace ArchivioLessicale.API.Features.Email.Domain;

public class GmailOptions
{
    public string SectionName { get; set; } = "GmailOptions";
    
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
}