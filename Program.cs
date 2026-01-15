using LevelSystem_WebServer.Data;
using LevelSystem_WebServer.Middleware;
using LevelSystem_WebServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ApplicationDbContextFactory>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<AchievementsService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SCP:SL Stats API",
        Version = "v1"
    });

    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key authentication",
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (args.Length > 0 && args[0] == "init-achievements")
{
    using var scope = app.Services.CreateScope();
    var statsService = scope.ServiceProvider.GetRequiredService<StatsService>();

    var servers = new[] { "7777", "7778", "7779", "7781" };

    foreach (var server in servers)
    {
        Console.WriteLine($"Initializing achievements for server {server}...");
        try
        {
            await statsService.InitializeAllPlayersAchievements(server);
            Console.WriteLine($"Successfully initialized achievements for server {server}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error for server {server}: {ex.Message}");
        }
    }

    Console.WriteLine("Achievements initialization completed!");
    return;
}

app.UseMiddleware<ApiKeyMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SCP:SL Stats API v1");

        c.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
    });
}

app.MapControllers();
InitializeDatabases(app);
app.Run();

static void InitializeDatabases(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<ApplicationDbContextFactory>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var serverPorts = configuration.GetSection("ServerConnections").GetChildren();

    foreach (var portConfig in serverPorts)
    {
        var dbContext = factory.CreateDbContext(portConfig.Key);
        dbContext.Database.EnsureCreated();
        Console.WriteLine($"Initialized database for server port: {portConfig.Key}");
    }
}