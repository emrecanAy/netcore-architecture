using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderSystem.Common;
using OrderSystem.Repositories.Abstract;
using OrderSystem.Repositories.Concrete;
using OrderSystem.Repositories.Interceptors;
using OrderSystem.Services;
using OrderSystem.Services.Behaviors;

namespace OrderSystem.Composition;

/// <summary>
/// Composition root. All DI registration converges here (PROJECT.md §9).
/// Handlers, validators and notification handlers are discovered by assembly
/// scanning, so adding a feature needs no manual registration here.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCompositionSetup(
        this IServiceCollection services,
        AppSettings appSettings,
        params Assembly[] assemblies)
    {
        services.AddSingleton(appSettings);

        // MediatR + pipeline. Registration order = wrapping order:
        // ValidationBehaviour (outer) → UnitOfWorkBehavior (inner, closest to handler).
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies.Distinct().ToArray()));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        services.AddValidatorsFromAssemblies(assemblies.Distinct(), ServiceLifetime.Scoped);

        // External integrations (email, ...).
        services.AddServices();

        // RAG help-desk integrations (AI provider, document ingestion).
        services.AddRag(appSettings);

        // Persistence. The connection interceptor loads the sqlite-vec extension so
        // the vector index (vec_chunks) is available on every connection.
        services.AddDbContext<AppDbContext>(options => options
            .UseSqlite(appSettings.ConnectionString)
            .AddInterceptors(new VecExtensionConnectionInterceptor(appSettings.Rag.VectorExtensionPath)));
        services.AddScoped(typeof(ISQLRepository<>), typeof(SqlRepository<>));
        services.AddScoped<IUnitOfWork, SQLUnitOfWork>();

        return services;
    }
}
