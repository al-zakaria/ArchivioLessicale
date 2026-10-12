using System.ComponentModel.DataAnnotations;

namespace ArchivioLessicale.API.Common.Links;

public class LinkOptions
{
    public const string SectionName  = "DeepLinks";

    [Required(ErrorMessage = "DeepLinks:BaseAddress is required")]
    [Url(ErrorMessage = "DeepLinks:BaseAddress is not valid")]
    public string BaseAddress { get; set; } = string.Empty;
}