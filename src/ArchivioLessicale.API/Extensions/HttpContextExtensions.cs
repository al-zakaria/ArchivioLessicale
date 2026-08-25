using ArchivioLessicale.API.Models.DTOs;

namespace ArchivioLessicale.API.Extensions;


public static class HttpContextExtensions
{
    public static ClientMetaData GetClientMetaData(this HttpContext context)
    {
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        }

        var userAgent = context.Request.Headers.UserAgent.ToString();

        return new ClientMetaData(
            UserIpAddress: ipAddress,
            UserAgent: string.IsNullOrWhiteSpace(userAgent) ? "Unknown User-Agent" : userAgent
        );
    }
}