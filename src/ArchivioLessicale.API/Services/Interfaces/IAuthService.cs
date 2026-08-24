using ArchivioLessicale.API.Models.DTOs;
using CSharpFunctionalExtensions;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> Register(RegisterRequest request, ClientMetaData clientMetaData);
    Task<Result<LoginResponse>> Login(LoginRequest request, ClientMetaData clientMetaData);
    Task<Result> ConfirmEmail(Guid userId, string encodedToken);
    Task<Result<LoginResponse>> ChangeEmail(Guid userId, string newEmail, string token, ClientMetaData clientMetaData);
    Task ResetPassword();
    Task DeleteAccount();
}
