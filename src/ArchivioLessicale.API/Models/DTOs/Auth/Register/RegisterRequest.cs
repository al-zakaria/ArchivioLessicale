using ArchivioLessicale.API.Models.Enums;

namespace ArchivioLessicale.API.Models.DTOs.Auth.Register;

public record RegisterRequest(
    string NickName, 
    string DisplayName,
    UserGrade Grade, 
    string Email, 
    string PhoneNumber, 
    string Password,
    string ConfirmPassword);
