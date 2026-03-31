using System.Text.Json;

namespace ContosoBank.Tests.Integration;

public class GrafanaDashboardTests
{
    private static readonly string DashboardPath = FindDashboardPath();

    private static string FindDashboardPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ContosoBank.slnx")))
            dir = dir.Parent;

        if (dir == null)
            throw new InvalidOperationException("Could not find repository root (looking for ContosoBank.slnx)");

        return Path.Combine(dir.FullName, "grafana", "dashboards", "contoso-bank.json");
    }

    [Fact]
    public void DashboardJson_FileExists()
    {
        Assert.True(File.Exists(DashboardPath), $"Dashboard JSON not found at: {DashboardPath}");
    }

    [Fact]
    public void DashboardJson_IsValidJson()
    {
        var json = File.ReadAllText(DashboardPath);
        var ex = Record.Exception(() => JsonDocument.Parse(json));
        Assert.Null(ex);
    }

    [Fact]
    public void DashboardJson_HasRequiredTopLevelFields()
    {
        using var doc = ParseDashboard();
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("title", out var title), "Missing 'title' field");
        Assert.False(string.IsNullOrEmpty(title.GetString()), "'title' should not be empty");

        Assert.True(root.TryGetProperty("panels", out var panels), "Missing 'panels' field");
        Assert.Equal(JsonValueKind.Array, panels.ValueKind);
        Assert.True(panels.GetArrayLength() > 0, "'panels' array should not be empty");

        Assert.True(root.TryGetProperty("templating", out _), "Missing 'templating' field");

        Assert.True(root.TryGetProperty("time", out var time), "Missing 'time' field");
        Assert.True(time.TryGetProperty("from", out _), "Missing 'time.from' field");
        Assert.True(time.TryGetProperty("to", out _), "Missing 'time.to' field");
    }

    [Fact]
    public void DashboardJson_HasValidSchemaVersion()
    {
        using var doc = ParseDashboard();
        Assert.True(doc.RootElement.TryGetProperty("schemaVersion", out var sv), "Missing 'schemaVersion'");
        Assert.True(sv.GetInt32() > 0, "'schemaVersion' should be positive");
    }

    [Fact]
    public void DashboardJson_DefaultTimeRangeIs15Minutes()
    {
        using var doc = ParseDashboard();
        var time = doc.RootElement.GetProperty("time");

        Assert.Equal("now-15m", time.GetProperty("from").GetString());
        Assert.Equal("now", time.GetProperty("to").GetString());
    }

    [Fact]
    public void DashboardJson_HasCollapsibleRowSections()
    {
        using var doc = ParseDashboard();
        var panels = doc.RootElement.GetProperty("panels");

        var rowTitles = panels.EnumerateArray()
            .Where(p => p.GetProperty("type").GetString() == "row")
            .Select(p => p.GetProperty("title").GetString()!)
            .ToList();

        Assert.Contains(rowTitles, t => t.Contains("Overview", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(rowTitles, t => t.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(rowTitles, t => t.Contains("Database", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(rowTitles, t => t.Contains("Business", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DashboardJson_HasTemplateVariables()
    {
        using var doc = ParseDashboard();
        var templating = doc.RootElement.GetProperty("templating").GetProperty("list");

        Assert.True(templating.GetArrayLength() > 0, "Should have at least one template variable");

        var varNames = templating.EnumerateArray()
            .Select(v => v.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("datasource", varNames);
        Assert.Contains("job", varNames);
    }

    [Fact]
    public void DashboardJson_PanelsHaveUniqueIds()
    {
        using var doc = ParseDashboard();
        var panels = doc.RootElement.GetProperty("panels");

        var ids = panels.EnumerateArray()
            .Select(p => p.GetProperty("id").GetInt32())
            .ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void DashboardJson_AllPanelsHaveGridPos()
    {
        using var doc = ParseDashboard();
        var panels = doc.RootElement.GetProperty("panels");

        foreach (var panel in panels.EnumerateArray())
        {
            var title = panel.GetProperty("title").GetString();
            Assert.True(panel.TryGetProperty("gridPos", out var gridPos),
                $"Panel '{title}' missing 'gridPos'");
            Assert.True(gridPos.TryGetProperty("x", out _), $"Panel '{title}' missing 'gridPos.x'");
            Assert.True(gridPos.TryGetProperty("y", out _), $"Panel '{title}' missing 'gridPos.y'");
            Assert.True(gridPos.TryGetProperty("w", out _), $"Panel '{title}' missing 'gridPos.w'");
            Assert.True(gridPos.TryGetProperty("h", out _), $"Panel '{title}' missing 'gridPos.h'");
        }
    }

    [Fact]
    public void DashboardJson_QueriesReferenceExpectedMetrics()
    {
        var json = File.ReadAllText(DashboardPath);

        // Custom business/reliability metrics from BankMetrics.cs
        Assert.Contains("contosobank_transfers_total", json);
        Assert.Contains("contosobank_transactions_processed_total", json);
        Assert.Contains("contosobank_db_errors_total", json);
        Assert.Contains("contosobank_dependency_timeouts_total", json);
        Assert.Contains("contosobank_exceptions_total", json);
        Assert.Contains("contosobank_log_entries_total", json);
        Assert.Contains("contosobank_db_connections_active", json);

        // Auto-instrumented metrics
        Assert.Contains("http_server_request_duration_seconds", json);
        Assert.Contains("process_cpu_seconds_total", json);
        Assert.Contains("process_working_set_bytes", json);
    }

    [Fact]
    public void DashboardJson_DataPanelsHaveTargets()
    {
        using var doc = ParseDashboard();
        var panels = doc.RootElement.GetProperty("panels");

        foreach (var panel in panels.EnumerateArray())
        {
            var type = panel.GetProperty("type").GetString();
            if (type == "row") continue;

            var title = panel.GetProperty("title").GetString();
            Assert.True(panel.TryGetProperty("targets", out var targets),
                $"Panel '{title}' missing 'targets'");
            Assert.True(targets.GetArrayLength() > 0,
                $"Panel '{title}' should have at least one target");
        }
    }

    private static JsonDocument ParseDashboard()
    {
        var json = File.ReadAllText(DashboardPath);
        return JsonDocument.Parse(json);
    }
}
