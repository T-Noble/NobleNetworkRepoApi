using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Swagger (Swashbuckle only)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy())
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection") ?? "",
        name: "sql",
        tags: new[] { "ready" }
    );

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Liveness
app.MapHealthChecks("/health/live");

app.MapGet("/api/health/live", () => Results.Ok(new { ok = true, status = "live" }))
   .WithTags("Health")
   .WithName("HealthLive");

// Readiness
app.MapGet("/api/health/ready", async (IConfiguration config) =>
{
    // Optional: do a lightweight DB check here if you want it to mirror readiness
    var cs = config.GetConnectionString("DefaultConnection");
    return string.IsNullOrWhiteSpace(cs)
        ? Results.Problem("DefaultConnection not set")
        : Results.Ok(new { ok = true, status = "ready" });
})
.WithTags("Health")
.WithName("HealthReady");

// Readiness (SQL)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}