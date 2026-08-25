using System.Net;
using ArchivioLessicale.API.Data;
using ArchivioLessicale.API.Services.Implementations;
using ArchivioLessicale.API.Services.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

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
                    Credentials = new NetworkCredential("your-proxy-username", "your-proxy-password")
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

    public static WebApplicationBuilder AddData(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("")));

        return builder;
    }

    public static WebApplicationBuilder AddStandardInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        
        return builder;
    }

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        
        builder.Services.AddSingleton<ILinkService, LinkService>();
        builder.Services.AddSingleton<IEmailTemplatesService, EmailTemplatesService>();
        
        builder.Services.AddTransient<IEmailService, EmailService>();
        
        return builder;
    }
    
    public static WebApplicationBuilder AddApplicationAbstractions(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        
        return builder;
    }
}