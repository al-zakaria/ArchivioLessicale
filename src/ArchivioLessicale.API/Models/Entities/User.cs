using ArchivioLessicale.API.Models.Enums;

namespace ArchivioLessicale.API.Models;

public class User
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? SecondName { get; set; }
    public UserGrade Grade { get; set; }
    public int NumberOfLearningWords { get; set; } = UserConstants.DefaultNumberLearningWords;
    public int NumberOfLearnedWords { get; set; } = UserConstants.DefaultNumberLearnedWords;
    public DateTime CreatedAt { get; set; }
}