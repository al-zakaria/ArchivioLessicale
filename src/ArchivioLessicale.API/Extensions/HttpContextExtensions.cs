using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Models.DTOs.Auth;

namespace ArchivioLessicale.API.Extensions;


public static class HttpContextExtensions
{
    public static ClientMetaData GetClientMetaData(this HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress.ToString();

        var userAgent = context.Request.Headers.UserAgent.ToString();

        return new ClientMetaData(
            UserIpAddress: ipAddress,
            UserAgent: string.IsNullOrWhiteSpace(userAgent) ? "Unknown User-Agent" : userAgent
        );
    }
}