using ArchivioLessicale.API.Models.DTOs;
using CSharpFunctionalExtensions;

namespace ArchivioLessicale.API.Services.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> Register(RegisterRequest request, ClientMetaData clientMetaData);
    Task<Result<LoginResponse>> Login(LoginRequest request, ClientMetaData clientMetaData);
    Task<LoginResponse> RefreshSession(string incomingRefreshToken, ClientMetaData clientMetaData);
    Task<string> UpdateSession(string incomingRefreshToken, ClientMetaData clientMetaData);
    Task<Result> ConfirmEmail(Guid userId, string encodedToken);
    Task RequestEmailConfirmation(Guid userId);
    Task<Result<LoginResponse>> ChangeEmail(Guid userId, string newEmail, string token, ClientMetaData clientMetaData);
    Task<Result<string>> RequestEmailChange(Guid userId, string newEmail, string password);

    Task<LoginResponse> CancelEmailChange(Guid userId,
        string rawCancellationEmailChangeToken, string rawRefreshToken, ClientMetaData clientMetaData);
    Task ResetPassword();
    Task DeleteAccount();
}
