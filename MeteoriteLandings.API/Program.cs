using MeteoriteLandings.Application.MappingProfiles;
using MeteoriteLandings.Application.Services;
using MeteoriteLandings.Infrastructure.Clients;
using MeteoriteLandings.Infrastructure.Data;
using MeteoriteLandings.Infrastructure.Repositories;
using MeteoriteLandings.Application.Repositories;
using MeteoriteLandings.Infrastructure.Services;
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
app.MapControllers();

app.Run();
