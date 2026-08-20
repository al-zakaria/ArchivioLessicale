using ArchivioLessicale.API.Models.Enums;

namespace ArchivioLessicale.API.Models.DTOs;

public record RegisterRequest(
    string FirstName, 
    string SecondName, 
    UserGrade Grade, 
    string Email, 
    string PhoneNumber, 
    string Password,
    string ConfirmPassword);
