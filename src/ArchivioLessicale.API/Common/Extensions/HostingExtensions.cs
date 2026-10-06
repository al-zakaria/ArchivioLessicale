using System.Text;
using ArchivioLessicale.API.Features.Auth.Tokens;
using FluentValidation;
using ImTools;
using JasperFx.CodeGeneration;
using JasperFx.Resources;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Postgresql;

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
            ?? throw new InvalidOperationException("JwtOptions section not found in appsetting(Devolopment).json");

        builder.Services.AddSingleton(jwtOptions);

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
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        return builder;
    }

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<TokenProvider>();

        return builder;
    }

    public static WebApplicationBuilder AddWolverine(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Host.UseWolverine(options =>
        {
            options.PersistMessagesWithPostgresql(connectionString!, schemaName: "wolverine");

            options.UseEntityFrameworkCoreTransactions();
            options.Policies.AutoApplyTransactions();

            options.UseFluentValidation();

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
