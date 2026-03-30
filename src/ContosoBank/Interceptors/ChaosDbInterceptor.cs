using ContosoBank.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace ContosoBank.Interceptors;

public class ChaosDbInterceptor : DbConnectionInterceptor
{
    private readonly IChaosService _chaos;

    public ChaosDbInterceptor(IChaosService chaos)
    {
        _chaos = chaos;
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        if (_chaos.GetStatus().IsDbFailureActive)
            throw new InvalidOperationException("Database connection failed (simulated outage)");
        return base.ConnectionOpening(connection, eventData, result);
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection, ConnectionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    {
        if (_chaos.GetStatus().IsDbFailureActive)
            throw new InvalidOperationException("Database connection failed (simulated outage)");
        return base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    }
}
