using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OOs.Extensions.Hosting;

internal sealed partial class ApplicationInitService(
    IServiceProvider services, ILogger<ApplicationInitService> logger) :
    IHostedService, IHostedLifecycleService
{
#pragma warning disable CA1823 // Avoid unused private fields
    private readonly ILogger logger = logger;
#pragma warning restore CA1823 // Avoid unused private fields

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(1, LogLevel.Information, $"Started {nameof(ApplicationInitService)} service")]
    private partial void LogStarted();

    [LoggerMessage(2, LogLevel.Information, "Starting '{initializerName}' initializer")]
    private partial void LogStartInit(string initializerName);

    [LoggerMessage(3, LogLevel.Information, "Initialization is done")]
    private partial void LogDone();

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        LogStarted();
        using var scope = services.CreateScope();
        var initializers = scope.ServiceProvider.GetServices<IServiceInitializer>();
        foreach (var initializer in initializers)
        {
            LogStartInit(initializer.GetType().Name);
            await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        LogDone();
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public interface IServiceInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}