using ContosoBank.Metrics;
using ContosoBank.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.Metrics;

namespace ContosoBank.Tests.Services;

public class ChaosServiceTests : IDisposable
{
    private readonly ChaosService _chaosService;
    private readonly Mock<ILogger<ChaosService>> _mockLogger;
    private readonly BankMetrics _metrics;
    private readonly IMeterFactory _meterFactory;

    public ChaosServiceTests()
    {
        _mockLogger = new Mock<ILogger<ChaosService>>();
        _meterFactory = new TestMeterFactory();
        _metrics = new BankMetrics(_meterFactory);
        _chaosService = new ChaosService(_mockLogger.Object, _metrics);
    }

    public void Dispose()
    {
        (_meterFactory as IDisposable)?.Dispose();
    }

    // --- GetStatus ---

    [Fact]
    public void GetStatus_InitiallyAllInactive()
    {
        var status = _chaosService.GetStatus();

        Assert.False(status.IsMemoryLeakActive);
        Assert.False(status.IsCpuSpikeActive);
        Assert.False(status.IsHttpErrorsActive);
        Assert.False(status.IsDbFailureActive);
        Assert.False(status.IsSlowResponsesActive);
        Assert.False(status.IsDependencyTimeoutActive);
        Assert.False(status.IsLogFloodingActive);
        Assert.False(status.IsExceptionStormActive);
        Assert.Empty(status.ActiveScenarios);
    }

    // --- Scenario 1: Memory Leak ---

    [Fact]
    public async Task TriggerMemoryLeak_ActivatesScenario()
    {
        await _chaosService.TriggerMemoryLeak(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsMemoryLeakActive);
        Assert.Contains("MemoryLeak", _chaosService.GetStatus().ActiveScenarios);
    }

    [Fact]
    public async Task TriggerMemoryLeak_AutoRecoversAfterDuration()
    {
        await _chaosService.TriggerMemoryLeak(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsMemoryLeakActive);
        await Task.Delay(4000);
        Assert.False(_chaosService.GetStatus().IsMemoryLeakActive);
    }

    [Fact]
    public async Task TriggerMemoryLeak_DoesNotDoubleActivate()
    {
        await _chaosService.TriggerMemoryLeak(TimeSpan.FromSeconds(2));
        await _chaosService.TriggerMemoryLeak(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsMemoryLeakActive);
    }

    // --- Scenario 2: CPU Spike ---

    [Fact]
    public async Task TriggerCpuSpike_ActivatesScenario()
    {
        await _chaosService.TriggerCpuSpike(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsCpuSpikeActive);
        Assert.Contains("CpuSpike", _chaosService.GetStatus().ActiveScenarios);
    }

    [Fact]
    public async Task TriggerCpuSpike_AutoRecoversAfterDuration()
    {
        await _chaosService.TriggerCpuSpike(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsCpuSpikeActive);
        await Task.Delay(4000);
        Assert.False(_chaosService.GetStatus().IsCpuSpikeActive);
    }

    // --- Scenario 3: HTTP Errors ---

    [Fact]
    public async Task TriggerHttpErrors_ActivatesScenario()
    {
        await _chaosService.TriggerHttpErrors(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsHttpErrorsActive);
        Assert.Contains("HttpErrors", _chaosService.GetStatus().ActiveScenarios);
    }

    [Fact]
    public async Task TriggerHttpErrors_AutoRecoversAfterDuration()
    {
        await _chaosService.TriggerHttpErrors(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsHttpErrorsActive);
        await Task.Delay(4000);
        Assert.False(_chaosService.GetStatus().IsHttpErrorsActive);
    }

    // --- Scenario 4: DB Connection Failure ---

    [Fact]
    public async Task TriggerDbConnectionFailure_ActivatesScenario()
    {
        await _chaosService.TriggerDbConnectionFailure(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsDbFailureActive);
        Assert.Contains("DbFailure", _chaosService.GetStatus().ActiveScenarios);
    }

    [Fact]
    public async Task TriggerDbConnectionFailure_AutoRecoversAfterDuration()
    {
        await _chaosService.TriggerDbConnectionFailure(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsDbFailureActive);
        await Task.Delay(4000);
        Assert.False(_chaosService.GetStatus().IsDbFailureActive);
    }

    // --- Scenario 5: Slow Responses ---

    [Fact]
    public async Task TriggerSlowResponses_ActivatesScenario()
    {
        await _chaosService.TriggerSlowResponses(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsSlowResponsesActive);
        Assert.Contains("SlowResponses", _chaosService.GetStatus().ActiveScenarios);
    }

    [Fact]
    public async Task TriggerSlowResponses_AutoRecoversAfterDuration()
    {
        await _chaosService.TriggerSlowResponses(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsSlowResponsesActive);
        await Task.Delay(4000);
        Assert.False(_chaosService.GetStatus().IsSlowResponsesActive);
    }

    // --- Scenario 6: Dependency Timeout ---

    [Fact]
    public async Task TriggerDependencyTimeout_ActivatesScenario()
    {
        await _chaosService.TriggerDependencyTimeout(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsDependencyTimeoutActive);
        Assert.Contains("DependencyTimeout", _chaosService.GetStatus().ActiveScenarios);
    }

    [Fact]
    public async Task TriggerDependencyTimeout_AutoRecoversAfterDuration()
    {
        await _chaosService.TriggerDependencyTimeout(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsDependencyTimeoutActive);
        await Task.Delay(4000);
        Assert.False(_chaosService.GetStatus().IsDependencyTimeoutActive);
    }

    // --- Scenario 7: Log Flooding ---

    [Fact]
    public async Task TriggerLogFlooding_ActivatesScenario()
    {
        await _chaosService.TriggerLogFlooding(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsLogFloodingActive);
        Assert.Contains("LogFlooding", _chaosService.GetStatus().ActiveScenarios);
    }

    [Fact]
    public async Task TriggerLogFlooding_AutoRecoversAfterDuration()
    {
        await _chaosService.TriggerLogFlooding(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsLogFloodingActive);
        await Task.Delay(4000);
        Assert.False(_chaosService.GetStatus().IsLogFloodingActive);
    }

    // --- Scenario 8: Exception Storm ---

    [Fact]
    public async Task TriggerExceptionStorm_ActivatesScenario()
    {
        await _chaosService.TriggerExceptionStorm(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsExceptionStormActive);
        Assert.Contains("ExceptionStorm", _chaosService.GetStatus().ActiveScenarios);
    }

    [Fact]
    public async Task TriggerExceptionStorm_AutoRecoversAfterDuration()
    {
        await _chaosService.TriggerExceptionStorm(TimeSpan.FromSeconds(2));

        Assert.True(_chaosService.GetStatus().IsExceptionStormActive);
        await Task.Delay(4000);
        Assert.False(_chaosService.GetStatus().IsExceptionStormActive);
    }

    // --- Concurrent activation ---

    [Fact]
    public async Task MultipleScenarios_CanBeActiveSimultaneously()
    {
        await _chaosService.TriggerHttpErrors(TimeSpan.FromSeconds(2));
        await _chaosService.TriggerSlowResponses(TimeSpan.FromSeconds(2));
        await _chaosService.TriggerDbConnectionFailure(TimeSpan.FromSeconds(2));

        var status = _chaosService.GetStatus();
        Assert.True(status.IsHttpErrorsActive);
        Assert.True(status.IsSlowResponsesActive);
        Assert.True(status.IsDbFailureActive);
        Assert.Equal(3, status.ActiveScenarios.Count);
    }

    [Fact]
    public async Task ConcurrentActivation_IsThreadSafe()
    {
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            Task.Run(async () =>
            {
                await _chaosService.TriggerHttpErrors(TimeSpan.FromSeconds(2));
                await _chaosService.TriggerCpuSpike(TimeSpan.FromSeconds(2));
                await _chaosService.TriggerMemoryLeak(TimeSpan.FromSeconds(2));
            }));

        await Task.WhenAll(tasks);

        // All should be active without exceptions
        var status = _chaosService.GetStatus();
        Assert.True(status.IsHttpErrorsActive);
        Assert.True(status.IsCpuSpikeActive);
        Assert.True(status.IsMemoryLeakActive);
    }

    // --- Logging ---

    [Fact]
    public async Task TriggerScenario_LogsActivationAtWarningLevel()
    {
        await _chaosService.TriggerHttpErrors(TimeSpan.FromSeconds(2));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("ChaosScenario activated")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // --- Test helper ---

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters) meter.Dispose();
            _meters.Clear();
        }
    }
}
