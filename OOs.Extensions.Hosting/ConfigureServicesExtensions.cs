using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace OOs.Extensions.Hosting;

public static class ConfigureServicesExtensions
{
    public static IServiceCollection AddServicesInit(this IServiceCollection services) =>
        services.AddHostedService<ApplicationInitService>();

    public static IServiceCollection AddServiceInitializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>
        (this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        where T : IServiceInitializer
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Add(new ServiceDescriptor(typeof(IServiceInitializer), typeof(T), lifetime));
        return services;
    }

    public static IServiceCollection AddServiceInitializer(this IServiceCollection services, Func<CancellationToken, Task> initializer,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(IServiceInitializer), sp => new InitializeWrapper(initializer), lifetime));
        return services;
    }

    public static IServiceCollection AddServiceInitializer<TDep>(this IServiceCollection services, Func<TDep, CancellationToken, Task> initializer,
        ServiceLifetime lifetime = ServiceLifetime.Transient) where TDep : notnull
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(IServiceInitializer), sp => new InitializeWrapper<TDep>(sp.GetRequiredService<TDep>(), initializer), lifetime));
        return services;
    }

    public static IServiceCollection AddServiceInitializer<TDep1, TDep2>(this IServiceCollection services, Func<TDep1, TDep2, CancellationToken, Task> initializer,
        ServiceLifetime lifetime = ServiceLifetime.Transient) where TDep1 : notnull where TDep2 : notnull
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(IServiceInitializer), sp => new InitializeWrapper<TDep1, TDep2>(sp.GetRequiredService<TDep1>(), sp.GetRequiredService<TDep2>(), initializer), lifetime));
        return services;
    }

    public static IServiceCollection AddCertificateGenInitializer(this IServiceCollection services) =>
        services.AddTransient<IServiceInitializer, CertificateGenerateInitializer>();
}

internal sealed class InitializeWrapper(Func<CancellationToken, Task> initializer) : IServiceInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken) => initializer(cancellationToken);
}

internal sealed class InitializeWrapper<TDep>(TDep dependency, Func<TDep, CancellationToken, Task> initializer) : IServiceInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken) => initializer(dependency, cancellationToken);
}

internal sealed class InitializeWrapper<TDep1, TDep2>(TDep1 dependency1, TDep2 dependency2, Func<TDep1, TDep2, CancellationToken, Task> initializer) : IServiceInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken) => initializer(dependency1, dependency2, cancellationToken);
}