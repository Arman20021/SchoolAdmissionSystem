
using Admission.BLL.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Admission.Api.Services;

public sealed class HoldExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpirationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;

    public HoldExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpirationBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var intervalSeconds =
            configuration.GetValue<int?>(
                "AdmissionHold:CheckIntervalSeconds"
            ) ?? 30;

        if (intervalSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Hold expiration check interval must be greater than zero."
            );
        }

        _checkInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Hold expiration background service started."
        );

        // Run once immediately when the service starts.
        await RunExpirationCycleAsync(stoppingToken);

        using var timer = new PeriodicTimer(_checkInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunExpirationCycleAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Graceful application shutdown.
        }

        _logger.LogInformation(
            "Hold expiration background service stopped."
        );
    }

    private async Task RunExpirationCycleAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var repository =
                scope.ServiceProvider.GetRequiredService<
                    IAdmissionHoldRepository
                >();

            var currentUtcTime = DateTimeOffset.UtcNow;

            var expiredCount =
                await repository.ExpireHoldsAsync(
                    currentUtcTime,
                    cancellationToken
                );

            if (expiredCount > 0)
            {
                _logger.LogInformation(
                    "{Count} admission holds expired at {Time}.",
                    expiredCount,
                    currentUtcTime
                );
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred during hold expiration."
            );
        }
    }
}
