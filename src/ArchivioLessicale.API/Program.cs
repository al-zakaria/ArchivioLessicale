using ArchivioLessicale.API.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

var (_, jwtOptions) = builder.AddOptions();
builder.AddData()
    .AddAuth(jwtOptions)
    .AddApplicationServices()
    .AddFluentValidation()
    .AddWolverine()
    .AddStandardConfiguration();

var app = builder.Build();

app.UseWebApplicationPipeline();

app.Run();
