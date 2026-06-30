using Microsoft.EntityFrameworkCore;
using OrderSystem.Api.Middlewares;
using OrderSystem.Commands.Products.CreateProduct;
using OrderSystem.Common;
using OrderSystem.Composition;
using OrderSystem.Queries.Products.GetProduct;
using OrderSystem.Repositories.Concrete;

var builder = WebApplication.CreateBuilder(args);

var appSettings = builder.Configuration.GetSection(AppSettings.SectionName).Get<AppSettings>()
                  ?? new AppSettings();

// The Gemini key is a secret: it comes from the environment / user-secrets,
// never from appsettings.
appSettings.Rag.ApiKey = builder.Configuration["GEMINI_API_KEY"] ?? appSettings.Rag.ApiKey;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Single composition root. Scan the Commands and Queries assemblies for
// handlers, validators and domain-event handlers.
builder.Services.AddCompositionSetup(
    appSettings,
    typeof(CreateProductCommand).Assembly,
    typeof(GetProductQuery).Assembly);

var app = builder.Build();

// Migration is a deliberate, separate step (PROJECT.md §15.7): run with --migrate.
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    return;
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

await app.RunAsync();
