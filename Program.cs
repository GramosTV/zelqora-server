using System.Text;
using HealthcareApi.Data;
using HealthcareApi.Middleware;
using HealthcareApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add database context - MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS to allow requests from Angular frontend and test pages
builder.Services.AddCors(options =>
{
    // Policy for Angular app with credentials
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200", "http://localhost:4201" };

    options.AddPolicy("AllowAngularApp",
        builder => builder
            .WithOrigins(corsOrigins) // Use configuration values
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());

    // Policy for testing with file:// protocol (no credentials needed)
    options.AddPolicy("AllowAll",
        builder => builder
            .SetIsOriginAllowed(_ => true) // Allow any origin
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT Key is not configured");
        }
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "HealthcareApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "HealthcareApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Add event handlers for debugging authentication issues
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("Authentication failed: {Exception}", context.Exception);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated successfully for {User}", context.Principal?.Identity?.Name ?? "unknown user");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                if (string.IsNullOrEmpty(context.Token))
                {
                    logger.LogWarning("No token found in request");
                }
                return Task.CompletedTask;
            }
        };
    });

// Register services
builder.Services.AddScoped<HealthcareApi.Services.IAuthService, HealthcareApi.Services.AuthService>();
builder.Services.AddScoped<HealthcareApi.Services.IUserService, HealthcareApi.Services.UserService>();
builder.Services.AddScoped<HealthcareApi.Services.IAppointmentService, HealthcareApi.Services.AppointmentService>();
builder.Services.AddScoped<HealthcareApi.Services.IMessageService, HealthcareApi.Services.MessageService>();
builder.Services.AddScoped<HealthcareApi.Services.IReminderService, HealthcareApi.Services.ReminderService>();
builder.Services.AddScoped<HealthcareApi.Services.ITokenService, HealthcareApi.Services.TokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use our new CORS policy for testing and development
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowAngularApp");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Create and seed database if needed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // For development, you can use EnsureDeleted() followed by EnsureCreated() during development
        // to recreate the database when model changes. Comment this out in production!
        if (app.Environment.IsDevelopment())
        {
            // Be careful! This will delete the database.
            // context.Database.EnsureDeleted();
        }

        // Create the database if it doesn't exist
        context.Database.Migrate();

        // Seed with initial data
        DbInitializer.Initialize(context);

        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database initialized successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();
