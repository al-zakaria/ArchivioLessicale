using ArchivioLessicale.API.Endpoints.Filters;
using ArchivioLessicale.API.Models.DTOs;
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

    private static Task<IResult> RegisterAsync()
    {
        throw new NotImplementedException();
    }
}