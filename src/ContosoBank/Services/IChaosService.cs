namespace ContosoBank.Services;

public interface IChaosService
{
    Task TriggerMemoryLeak(TimeSpan? duration = null, CancellationToken ct = default);
    Task TriggerCpuSpike(TimeSpan? duration = null, CancellationToken ct = default);
    Task TriggerHttpErrors(TimeSpan? duration = null);
    Task TriggerDbConnectionFailure(TimeSpan? duration = null);
    Task TriggerSlowResponses(TimeSpan? duration = null);
    Task TriggerDependencyTimeout(TimeSpan? duration = null, CancellationToken ct = default);
    Task TriggerLogFlooding(TimeSpan? duration = null, CancellationToken ct = default);
    Task TriggerExceptionStorm(TimeSpan? duration = null, CancellationToken ct = default);
    ChaosStatus GetStatus();
}
