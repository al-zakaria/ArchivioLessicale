using ArchivioLessicale.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder
    .AddStandardInfrastructure()
    .AddData()
    .AddApplicationAbstractions()
    .AddApplicationServices()
    .ConfigureHttpClients()
    .AddFluentValidation();

var app = builder.Build();

if (app.Environment.IsDevelopment()) 
    app.MapOpenApi();

app.UseHttpsRedirection();

app.UseForwardedHeaders();

app.Run();
