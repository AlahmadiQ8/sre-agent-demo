using ContosoBank.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Error handling — RFC 7807 ProblemDetails for all API errors
builder.Services.AddProblemDetails();

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

app.Run();
