using ArchivioLessicale.API.Endpoints.Filters;
using ArchivioLessicale.API.Extensions;
using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Services.Interfaces;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace ArchivioLessicale.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");
        
        group.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IAuthService authService,
        HttpContext httpContext)
    {
        var clientMetaData = httpContext.GetClientMetaData();

        var result = await authService.Register(request, clientMetaData);
        
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.BadRequest(new { error = result.Error });
    }
}
