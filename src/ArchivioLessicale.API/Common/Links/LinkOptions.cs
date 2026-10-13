using System.ComponentModel.DataAnnotations;

namespace ArchivioLessicale.API.Common.Links;

public class LinksOptions
{
    public const string SectionName  = "LinksOptions";

    [Required(ErrorMessage = "LinksOptions:BaseAddress is required")]
    [Url(ErrorMessage = "LinksOptions:BaseAddress is not valid")]
    public string BaseAddress { get; set; } = string.Empty;
}