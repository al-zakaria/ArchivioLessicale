namespace ArchivioLessicale.API.Models.DTOs.Tokens;

public record RefreshTokensRequest(string RawTokenFromUser, bool IsTokensNeedRefresh);
