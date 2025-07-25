using MeteoriteLandings.Application.MappingProfiles;
using MeteoriteLandings.Application.Services;
using MeteoriteLandings.Infrastructure.Clients;
using MeteoriteLandings.Infrastructure.Data;
using MeteoriteLandings.Infrastructure.Repositories;
using MeteoriteLandings.Application.Repositories;
using MeteoriteLandings.Infrastructure.Services;
using MeteoriteLandings.Infrastructure.Configuration;
using MeteoriteLandings.Infrastructure.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using MeteoriteLandings.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// Configuration binding with validation
builder.Services.AddOptions<NasaApiOptions>()
    .Bind(builder.Configuration.GetSection(NasaApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IMeteoriteRepository, MeteoriteRepository>();
builder.Services.AddScoped<IMeteoriteService, MeteoriteService>();
builder.Services.AddScoped<ICacheClearer, MeteoriteService>();

// Register resilience services
builder.Services.AddScoped<RetryPolicyService>();
builder.Services.AddSingleton<CircuitBreakerService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "ready" })
    .AddCheck<NasaApiHealthCheck>("nasa-api", tags: new[] { "external", "ready" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"), tags: new[] { "ready" });

// Register HttpClient for NASA API Health Check
builder.Services.AddHttpClient<NasaApiHealthCheck>();

builder.Services.AddHttpClient<NasaApiClient>();

builder.Services.AddHostedService<DataSyncService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<DataSyncService>>();
    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    var syncIntervalMinutes = builder.Configuration.GetValue<int>("DataSyncService:SyncIntervalMinutes");
    if (syncIntervalMinutes <= 0) syncIntervalMinutes = 60;
    return new DataSyncService(logger, scopeFactory, syncIntervalMinutes);
});

builder.Services.AddAutoMapper(typeof(MeteoriteMappingProfile).Assembly);

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseHttpsRedirection();

app.UseCors("DefaultPolicy");

app.UseAuthorization();

// Health Check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                exception = e.Value.Exception?.Message
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Only basic liveness check
});

app.MapControllers();

app.Run();
