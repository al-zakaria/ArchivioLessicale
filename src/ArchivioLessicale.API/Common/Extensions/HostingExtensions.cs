using System.Text;
using ArchivioLessicale.API.Common.Contracts;
using ArchivioLessicale.API.Common.Extensions;
using ArchivioLessicale.API.Common.Links;
using ArchivioLessicale.API.Features.Auth.Tokens;
using ArchivioLessicale.API.Features.Email.Common;
using ArchivioLessicale.API.Features.Email.Domain;
using ErrorOr;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using JasperFx.CodeGeneration;
using JasperFx.Resources;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql; // ← AnyAsync берётся отсюда
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Postgresql;
using LinkOptions = Microsoft.AspNetCore.Routing.LinkOptions;

namespace ArchivioLessicale.API.Common.Extensions;

public static class HostingExtensions
{
    public static WebApplicationBuilder AddData(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options => 
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        return builder;
    }

    public static (WebApplicationBuilder Builder, JwtOptions options) AddOptions(this WebApplicationBuilder builder)
    {
        var jwtOptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JwtOptions section not found in appsettings(Devolopment).json");
        
        var linkOptions =  builder.Configuration.GetSection("LinkOptions").Get<LinkOptions>()
            ??  throw new InvalidOperationException("LinkOptions section not found in appsettings(Devolopment).json");
        
        var gmailOptions = builder.Configuration.GetSection("GmailOptions").Get<GmailOptions>()
            ??  throw new InvalidOperationException("GmailOptions section not found in appsettings(Devolopment).json");

        builder.Services.AddSingleton(jwtOptions);
        builder.Services.AddSingleton(gmailOptions);
        builder.Services.AddSingleton(linkOptions);

        return (builder, jwtOptions);
    }

    public static WebApplicationBuilder AddAuth(this WebApplicationBuilder builder, JwtOptions jwtOptions)
    {
        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            
            options.User.AllowedUserNameCharacters = 
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }

    public static WebApplicationBuilder AddFluentValidation(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

        return builder;
    }

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<GmailService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GmailOptions>>().Value;
    
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = options.ClientId, ClientSecret = options.ClientSecret },
                Scopes = [GmailService.Scope.GmailSend]
            });

            var credential = new UserCredential(flow, "User", new TokenResponse { RefreshToken = options.RefreshToken });

            return new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ArchivioLessicale Inc.",
            });
        });
        
        builder.Services.AddScoped<TokenProvider>();
        builder.Services.AddScoped<UserActionsTokenProvider>();
        builder.Services.AddScoped<LinkService>();
        builder.Services.AddTransient<EmailService>();

        return builder;
    }

    public static WebApplicationBuilder AddWolverine(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        
        builder.Services.AddWolverineHttp();

        builder.Host.UseWolverine(options =>
        {
            options.PersistMessagesWithPostgresql(connectionString!, schemaName: "wolverine");

            options.UseEntityFrameworkCoreTransactions();
            options.Policies.AutoApplyTransactions();
            
            options.UseFluentValidation();
            
            options.CodeGeneration.AlwaysUseServiceLocationFor<UserManager<ApplicationUser>>();

            options.Policies.OnException<NpgsqlException>()
                .RetryWithCooldown(
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(500),
                    TimeSpan.FromSeconds(2)
                );

            options.Policies.OnException<Exception>()
                .ScheduleRetry(TimeSpan.FromSeconds(5))
                .Then.ScheduleRetry(TimeSpan.FromSeconds(30))
                .Then.MoveToErrorQueue();

            options.Policies.UseDurableLocalQueues();

            options.LocalQueue("background-workers")
                .MaximumParallelMessages(5);

            options.PublishMessage<UserRegisteredEvent>()
                .ToLocalQueue("background-workers");

            if (builder.Environment.IsDevelopment())
            {
                options.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic;

                options.Services.AddResourceSetupOnStartup();
            }
            else
            {
                options.CodeGeneration.TypeLoadMode = TypeLoadMode.Auto;
            }

            options.Discovery.IncludeAssembly(typeof(Program).Assembly);
        });

        return builder;
    }

    public static WebApplicationBuilder AddStandardConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        { 
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return builder;
    }

    public static WebApplication UseWebApplicationPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        if (!app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapWolverineEndpoints(options =>
        {
            options.UseFluentValidationProblemDetailMiddleware();
        });

        return app;
    }
}