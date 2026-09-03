using ArchivioLessicale.API.Models.Enums;

namespace ArchivioLessicale.API.Models.Entities;

public class Profile
{
    public Guid Id { get; set; }
    public string NickName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserGrade Grade { get; set; }
    public int NumberOfLearningWords { get; set; } = UserConstants.DefaultNumberLearningWords;
    public int NumberOfLearnedWords { get; set; } = UserConstants.DefaultNumberLearnedWords;
    public DateTimeOffset CreatedAt { get; set; } 
}