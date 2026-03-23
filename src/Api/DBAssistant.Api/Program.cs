using DBAssistant.Api.Extensions;
using DBAssistant.Data.DependencyInjection;
using DBAssistant.Services.DependencyInjection;
using DBAssistant.UseCases.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddApiConfiguration();
builder.Services.AddUseCases();
builder.Services.AddData(builder.Configuration);
builder.Services.AddExternalServices(builder.Configuration);

var app = builder.Build();

app.UseApiConfiguration();

app.Run();

public partial class Program;
