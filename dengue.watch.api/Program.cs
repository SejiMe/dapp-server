using Microsoft.EntityFrameworkCore;
using dengue.watch.api.common.exceptions;
using dengue.watch.api.common.extensions;
using dengue.watch.api.common.services;
using dengue.watch.api.infrastructure.database;
using dengue.watch.api.infrastructure.ml;
using Scalar.AspNetCore;
using System.Reflection;
using Microsoft.Extensions.Options;
using Npgsql;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using dengue.watch.api.common.options;
using dengue.watch.api.common.extensions;


var builder = WebApplication.CreateBuilder(args);

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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add ML.NET services
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

// Discover and register features using reflection
builder.Services.DiscoverFeatures(Assembly.GetExecutingAssembly());


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
app.UseCors();

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
   .WithTags("Health");

app.Run();
