namespace ContosoBank.Services;

public class ChaosStatus
{
    public bool IsMemoryLeakActive { get; init; }
    public bool IsCpuSpikeActive { get; init; }
    public bool IsHttpErrorsActive { get; init; }
    public bool IsDbFailureActive { get; init; }
    public bool IsSlowResponsesActive { get; init; }
    public bool IsDependencyTimeoutActive { get; init; }
    public bool IsLogFloodingActive { get; init; }
    public bool IsExceptionStormActive { get; init; }

    public IReadOnlyList<string> ActiveScenarios
    {
        get
        {
            var scenarios = new List<string>();
            if (IsMemoryLeakActive) scenarios.Add("MemoryLeak");
            if (IsCpuSpikeActive) scenarios.Add("CpuSpike");
            if (IsHttpErrorsActive) scenarios.Add("HttpErrors");
            if (IsDbFailureActive) scenarios.Add("DbFailure");
            if (IsSlowResponsesActive) scenarios.Add("SlowResponses");
            if (IsDependencyTimeoutActive) scenarios.Add("DependencyTimeout");
            if (IsLogFloodingActive) scenarios.Add("LogFlooding");
            if (IsExceptionStormActive) scenarios.Add("ExceptionStorm");
            return scenarios;
        }
    }
}
