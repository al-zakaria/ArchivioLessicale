using ArchivioLessicale.API.Models.Enums;

namespace ArchivioLessicale.API.Models.DTOs;

public record RegisterResponse(Guid Id, string FirstName, string SecondName, UserGrade Grade, int NumberOfLearningWords, int NumberOfLearnedWords);