using Azure.Monitor.OpenTelemetry.AspNetCore;
using ContosoBank.Data;
using ContosoBank.Metrics;
using ContosoBank.Services;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Error handling — RFC 7807 ProblemDetails for all API errors
builder.Services.AddProblemDetails();

// Observability — custom business + reliability metrics
builder.Services.AddSingleton<BankMetrics>();

// Business services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Database — InMemory for development, SQL Server for production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<BankDbContext>(options =>
        options.UseInMemoryDatabase("ContosoBank"));
}
else
{
    builder.Services.AddDbContext<BankDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BankDbContext>();

// OpenTelemetry — unified observability pipeline
var otelBuilder = builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("contoso-bank"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddSource("ContosoBank"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddMeter("ContosoBank")
        .AddPrometheusExporter());

// Azure Monitor exporter — requires APPLICATIONINSIGHTS_CONNECTION_STRING
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    otelBuilder.UseAzureMonitor();
}

var app = builder.Build();

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    db.Database.EnsureCreated();
    SeedData.Initialize(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // liveness: always 200
});
app.MapHealthChecks("/health/ready"); // readiness: checks DB connectivity
app.MapPrometheusScrapingEndpoint(); // exposes /metrics

app.Run();
