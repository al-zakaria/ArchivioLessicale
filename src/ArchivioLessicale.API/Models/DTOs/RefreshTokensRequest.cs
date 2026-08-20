namespace ArchivioLessicale.API.Models.DTOs;

public record RefreshTokensRequest(string RawTokenFromUser, bool IsTokensNeedRefresh);
