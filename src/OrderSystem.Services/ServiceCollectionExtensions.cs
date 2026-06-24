using Microsoft.Extensions.DependencyInjection;
using OrderSystem.Services.Abstract;
using OrderSystem.Services.Concrete;

namespace OrderSystem.Services;

/// <summary>
/// Registers external integrations. Kept in the Services layer so the composition
/// root stays a thin aggregator (PROJECT.md §9).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();
        return services;
    }
}
