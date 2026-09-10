using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Models.DTOs.Auth;
using ArchivioLessicale.API.Models.DTOs.Auth.Login;
using ArchivioLessicale.API.Models.DTOs.Auth.Register;
using CSharpFunctionalExtensions;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, ClientMetaData clientMetaData);
    Task<LoginResponse> LoginAsync(LoginRequest request, ClientMetaData clientMetaData);
}
