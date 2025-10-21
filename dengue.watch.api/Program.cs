
using Scalar.AspNetCore;
using System.Reflection;
using Microsoft.Extensions.Options;

using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.ML;


var builder = WebApplication.CreateBuilder(args);

// Removes the default header kestrel from response
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);


// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/dengue-watch-api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100_000_000,
        rollOnFileSizeLimit: true)
    .CreateLogger();

builder.Host.UseSerilog();

#if DEBUG
     builder.WebHost.UseUrls("http://0.0.0.0:5000");
    builder.WebHost.UseKestrel(options =>
    {
        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
        options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
        
    });
#endif

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

// Add Scalar for API documentation
builder.Services.AddOpenApi();
// Add Quartz scheduler
builder.Services.AddQuartzExtension();

// Bind Postgres options
builder.Services.Configure<PostgresOptions>(builder.Configuration.GetSection(PostgresOptions.SectionName));

// Add Entity Framework with Npgsql
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var pgOptions = sp.GetRequiredService<IOptions<PostgresOptions>>().Value;
    var connectionString = pgOptions.ToSessionPoolingConnectionString();
    options.UseNpgsql(connectionString);
});


// Add SignalR
builder.Services.AddSignalR();

// Add ProblemDetails service
builder.Services.AddProblemDetails();

// Add Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


// Add the Prediction Engine
// builder.Services.AddMLPredictionEngine(builder.Environment);


// Add Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddFixedWindowLimiter("FixedHeavyProcessing", opt =>
    {
        opt.PermitLimit = 5;
        opt.QueueLimit = 10;
        opt.Window = TimeSpan.FromMinutes(5);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

});


// Add CORS
var corsOriginsString = builder.Configuration["Cors:AllowedOrigins"];

// Parse comma-separated origins
var corsOrigins = string.IsNullOrWhiteSpace(corsOriginsString)
    ? Array.Empty<string>()
    : corsOriginsString
        .Split(',')
        .Select(origin => origin.Trim())
        .Where(origin => !string.IsNullOrEmpty(origin))
        .ToArray();  
builder.Services.AddCors(options =>
{
    if (!corsOrigins.Any())
    {
        throw new InvalidOperationException(
            "CORS origins must be configured in production. " +
            "Set CORS_ORIGINS environment variable.");
    }

    options.AddPolicy("DefinedOrigins", policy =>
    {
        Log.Logger.Information("Production mode: CORS origins configured {origins}", corsOrigins);
            
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
    
    
});

// Add ML.NET services
// var mlContext = new MLContext();
// mlContext.ComponentCatalog.RegisterAssembly(typeof(IsWetWeekMappingFactory).Assembly);


builder.Services.AddMLServices();

// Add Supabase configuration and client
builder.Services.AddSupabaseOptions(builder.Configuration);
builder.Services.AddSupabaseClient();

// Add JWT token service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Add JWT Authentication
var supabaseSection = builder.Configuration.GetSection("Supabase");
var jwtSecret = supabaseSection["JwtSecret"];
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var supabaseUrl = supabaseSection["Url"];
            var key = Encoding.UTF8.GetBytes(jwtSecret);
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = $"{supabaseUrl?.TrimEnd('/')}/auth/v1",
                ValidateAudience = true,
                ValidAudience = "dengue-watch-api",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
}

builder.Services.AddAuthorization();
builder.Services.AddOpenMeteo();

// Add this first
builder.Services.AddStatisticsServiceExtensions();
builder.Services.AddCommonRepositories();

// Discover and register features using reflection (pass configuration)
builder.Services.DiscoverFeatures(Assembly.GetExecutingAssembly(), builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Dengue Watch API")
               .WithTheme(ScalarTheme.BluePlanet)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Use Global Exception Handler
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("DefinedOrigins");
  
// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Discover and map endpoints using reflection
app.DiscoverEndpoints(Assembly.GetExecutingAssembly());

// Discover and map SignalR hubs using reflection
app.DiscoverHubs(Assembly.GetExecutingAssembly());

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("Health")
   .RequireAuthorization();

app.Run();
