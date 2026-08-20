using System.Net;
using ArchivioLessicale.API.Services.Interfaces;
using FluentValidation;

namespace ArchivioLessicale.API.Extensions;

internal static class HostingExtensions
{
    public static WebApplicationBuilder ConfigureHttpClients(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IArticleParserService, IArticleParserService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.BaseAddress = new Uri(builder.Configuration.GetSection("ArticleParserService").Value!);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            );
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            Proxy = new WebProxy("https://your-proxy-address:port")
            {
                Credentials =  new NetworkCredential("your-proxy-username", "your-proxy-password")
            },
            UseProxy = true,
            AllowAutoRedirect = true,
            
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
        });
        
        return builder;
    }

    public static WebApplicationBuilder AddFluentValidation(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        return builder;
    }
}