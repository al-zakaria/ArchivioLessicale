using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Models.DTOs.Auth.Login;
using ArchivioLessicale.API.Services.Interfaces;

namespace ArchivioLessicale.API.Services.Implementations;

public class AuthService : IAuthService
{
    public Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        throw new NotImplementedException();
    }
}