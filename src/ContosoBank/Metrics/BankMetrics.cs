using System.Diagnostics.Metrics;

namespace ContosoBank.Metrics;

public class BankMetrics
{
    public static readonly string MeterName = "ContosoBank";

    private readonly Meter _meter;

    // Business Metrics
    public Counter<long> TransfersTotal { get; }
    public Counter<long> TransactionsProcessed { get; }
    public UpDownCounter<int> ActiveSessions { get; }

    // Reliability Metrics
    public Counter<long> DbErrors { get; }
    public Counter<long> DependencyTimeouts { get; }
    public Counter<long> Exceptions { get; }
    public Counter<long> LogEntries { get; }

    // Resource Metrics
    public UpDownCounter<int> DbConnectionsActive { get; }

    public BankMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        TransfersTotal = _meter.CreateCounter<long>(
            "contosobank.transfers",
            description: "Total transfers processed");

        TransactionsProcessed = _meter.CreateCounter<long>(
            "contosobank.transactions.processed",
            description: "Total transactions processed");

        ActiveSessions = _meter.CreateUpDownCounter<int>(
            "contosobank.sessions.active",
            description: "Active user sessions");

        DbErrors = _meter.CreateCounter<long>(
            "contosobank.db.errors",
            description: "Database errors");

        DependencyTimeouts = _meter.CreateCounter<long>(
            "contosobank.dependency.timeouts",
            description: "Dependency timeouts");

        Exceptions = _meter.CreateCounter<long>(
            "contosobank.exceptions",
            description: "Exceptions caught");

        LogEntries = _meter.CreateCounter<long>(
            "contosobank.log.entries",
            description: "Log entries emitted");

        DbConnectionsActive = _meter.CreateUpDownCounter<int>(
            "contosobank.db.connections.active",
            description: "Active database connections");
    }
}
