using System.Collections.Concurrent;
using System.Security.Cryptography;
using ContosoBank.Metrics;

namespace ContosoBank.Services;

public class ChaosService : IChaosService
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);

    private readonly ILogger<ChaosService> _logger;
    private readonly BankMetrics _metrics;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeScenarios = new();

    // Held references to prevent GC from reclaiming leaked memory
    private readonly ConcurrentBag<byte[]> _leakedMemory = [];

    public ChaosService(ILogger<ChaosService> logger, BankMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public ChaosStatus GetStatus() => new()
    {
        IsMemoryLeakActive = _activeScenarios.ContainsKey("MemoryLeak"),
        IsCpuSpikeActive = _activeScenarios.ContainsKey("CpuSpike"),
        IsHttpErrorsActive = _activeScenarios.ContainsKey("HttpErrors"),
        IsDbFailureActive = _activeScenarios.ContainsKey("DbFailure"),
        IsSlowResponsesActive = _activeScenarios.ContainsKey("SlowResponses"),
        IsDependencyTimeoutActive = _activeScenarios.ContainsKey("DependencyTimeout"),
        IsLogFloodingActive = _activeScenarios.ContainsKey("LogFlooding"),
        IsExceptionStormActive = _activeScenarios.ContainsKey("ExceptionStorm"),
    };

    // --- Scenario 1: Memory Leak ---

    public Task TriggerMemoryLeak(TimeSpan? duration = null, CancellationToken ct = default)
    {
        var dur = duration ?? DefaultDuration;
        if (!TryActivate("MemoryLeak", dur, out var cts))
            return Task.CompletedTask;

        _logger.LogWarning("ChaosScenario activated: {ScenarioName} duration={Duration}s", "MemoryLeak", dur.TotalSeconds);

        // Start background work that allocates memory
        _ = Task.Run(async () =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
            try
            {
                while (!linked.Token.IsCancellationRequested)
                {
                    // Allocate 1 MB chunks and hold references
                    var chunk = new byte[1024 * 1024];
                    RandomNumberGenerator.Fill(chunk);
                    _leakedMemory.Add(chunk);
                    await Task.Delay(100, linked.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Clear leaked memory on recovery
                _leakedMemory.Clear();
                Deactivate("MemoryLeak");
                _logger.LogInformation("ChaosScenario recovered: {ScenarioName}", "MemoryLeak");
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    // --- Scenario 2: CPU Spike ---

    public Task TriggerCpuSpike(TimeSpan? duration = null, CancellationToken ct = default)
    {
        var dur = duration ?? DefaultDuration;
        if (!TryActivate("CpuSpike", dur, out var cts))
            return Task.CompletedTask;

        _logger.LogWarning("ChaosScenario activated: {ScenarioName} duration={Duration}s", "CpuSpike", dur.TotalSeconds);

        // Spawn CPU-intensive work on multiple threads
        var coreCount = Math.Max(Environment.ProcessorCount, 1);
        for (int i = 0; i < coreCount; i++)
        {
            _ = Task.Run(() =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
                try
                {
                    // Tight loop computing hash permutations (simulating ML fraud model)
                    while (!linked.Token.IsCancellationRequested)
                    {
                        SHA256.HashData(new byte[1024]);
                    }
                }
                catch (OperationCanceledException) { }
            }, CancellationToken.None);
        }

        // Recovery logging (only one thread does cleanup)
        _ = Task.Run(async () =>
        {
            try { await Task.Delay(dur, cts.Token); } catch (OperationCanceledException) { }
            Deactivate("CpuSpike");
            _logger.LogInformation("ChaosScenario recovered: {ScenarioName}", "CpuSpike");
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    // --- Scenario 3: HTTP 500 Errors ---

    public Task TriggerHttpErrors(TimeSpan? duration = null)
    {
        var dur = duration ?? DefaultDuration;
        if (!TryActivate("HttpErrors", dur, out var cts))
            return Task.CompletedTask;

        _logger.LogWarning("ChaosScenario activated: {ScenarioName} duration={Duration}s", "HttpErrors", dur.TotalSeconds);
        _metrics.Exceptions.Add(1, new KeyValuePair<string, object?>("exception_type", "InvalidOperationException"));

        ScheduleRecovery("HttpErrors", cts);
        return Task.CompletedTask;
    }

    // --- Scenario 4: Database Connection Failure ---

    public Task TriggerDbConnectionFailure(TimeSpan? duration = null)
    {
        var dur = duration ?? DefaultDuration;
        if (!TryActivate("DbFailure", dur, out var cts))
            return Task.CompletedTask;

        _logger.LogWarning("ChaosScenario activated: {ScenarioName} duration={Duration}s", "DbFailure", dur.TotalSeconds);
        _metrics.DbErrors.Add(1, new KeyValuePair<string, object?>("error_type", "connection_failure"));

        ScheduleRecovery("DbFailure", cts);
        return Task.CompletedTask;
    }

    // --- Scenario 5: Slow API / High Latency ---

    public Task TriggerSlowResponses(TimeSpan? duration = null)
    {
        var dur = duration ?? DefaultDuration;
        if (!TryActivate("SlowResponses", dur, out var cts))
            return Task.CompletedTask;

        _logger.LogWarning("ChaosScenario activated: {ScenarioName} duration={Duration}s", "SlowResponses", dur.TotalSeconds);

        ScheduleRecovery("SlowResponses", cts);
        return Task.CompletedTask;
    }

    // --- Scenario 6: Dependency Timeout ---

    public Task TriggerDependencyTimeout(TimeSpan? duration = null, CancellationToken ct = default)
    {
        var dur = duration ?? DefaultDuration;
        if (!TryActivate("DependencyTimeout", dur, out var cts))
            return Task.CompletedTask;

        _logger.LogWarning("ChaosScenario activated: {ScenarioName} duration={Duration}s", "DependencyTimeout", dur.TotalSeconds);
        _metrics.DependencyTimeouts.Add(1, new KeyValuePair<string, object?>("dependency", "kyc_provider"));

        // Recovery logging
        _ = Task.Run(async () =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
            try { await Task.Delay(dur, linked.Token); } catch (OperationCanceledException) { }
            Deactivate("DependencyTimeout");
            _logger.LogInformation("ChaosScenario recovered: {ScenarioName}", "DependencyTimeout");
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    // --- Scenario 7: Log Flooding ---

    public Task TriggerLogFlooding(TimeSpan? duration = null, CancellationToken ct = default)
    {
        var dur = duration ?? DefaultDuration;
        if (!TryActivate("LogFlooding", dur, out var cts))
            return Task.CompletedTask;

        _logger.LogWarning("ChaosScenario activated: {ScenarioName} duration={Duration}s", "LogFlooding", dur.TotalSeconds);

        _ = Task.Run(async () =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
            try
            {
                int index = 0;
                while (!linked.Token.IsCancellationRequested)
                {
                    _logger.LogDebug("Transaction batch item {Index}: account={AccountId} amount={Amount} hash={Hash}",
                        index++, Random.Shared.Next(1, 100), Random.Shared.NextDouble() * 10000, Guid.NewGuid());
                    _metrics.LogEntries.Add(1, new KeyValuePair<string, object?>("level", "Debug"));
                    await Task.Delay(1, linked.Token); // ~1000 entries/second
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                Deactivate("LogFlooding");
                _logger.LogInformation("ChaosScenario recovered: {ScenarioName}", "LogFlooding");
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    // --- Scenario 8: Exception Storm ---

    public Task TriggerExceptionStorm(TimeSpan? duration = null, CancellationToken ct = default)
    {
        var dur = duration ?? DefaultDuration;
        if (!TryActivate("ExceptionStorm", dur, out var cts))
            return Task.CompletedTask;

        _logger.LogWarning("ChaosScenario activated: {ScenarioName} duration={Duration}s", "ExceptionStorm", dur.TotalSeconds);

        _ = Task.Run(async () =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
            var exceptionTypes = new[] { "NullReferenceException", "ArgumentException", "DivideByZeroException", "FormatException" };
            try
            {
                while (!linked.Token.IsCancellationRequested)
                {
                    var exType = exceptionTypes[Random.Shared.Next(exceptionTypes.Length)];
                    Exception ex = exType switch
                    {
                        "NullReferenceException" => new NullReferenceException("Batch item reference was null"),
                        "ArgumentException" => new ArgumentException("Invalid batch reconciliation parameter"),
                        "DivideByZeroException" => new DivideByZeroException("Division by zero in balance calculation"),
                        "FormatException" => new FormatException("Invalid transaction format in batch"),
                        _ => new Exception("Unknown batch error")
                    };

                    _logger.LogError(ex, "Batch reconciliation error: {ExceptionType}", exType);
                    _metrics.Exceptions.Add(1, new KeyValuePair<string, object?>("exception_type", exType));
                    await Task.Delay(50, linked.Token); // ~20 exceptions/second
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                Deactivate("ExceptionStorm");
                _logger.LogInformation("ChaosScenario recovered: {ScenarioName}", "ExceptionStorm");
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    // --- Activation / Deactivation Helpers ---

    private bool TryActivate(string scenario, TimeSpan duration, out CancellationTokenSource cts)
    {
        cts = new CancellationTokenSource(duration);
        if (!_activeScenarios.TryAdd(scenario, cts))
        {
            // Already active — don't double-trigger
            cts.Dispose();
            _logger.LogInformation("ChaosScenario already active: {ScenarioName}", scenario);
            return false;
        }
        return true;
    }

    private void Deactivate(string scenario)
    {
        if (_activeScenarios.TryRemove(scenario, out var cts))
        {
            cts.Dispose();
        }
    }

    private void ScheduleRecovery(string scenario, CancellationTokenSource cts)
    {
        _ = Task.Run(async () =>
        {
            try { await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token); }
            catch (OperationCanceledException) { }
            finally
            {
                Deactivate(scenario);
                _logger.LogInformation("ChaosScenario recovered: {ScenarioName}", scenario);
            }
        }, CancellationToken.None);
    }
}
