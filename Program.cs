using System.Text;
using FluentValidation;
using HealthcareApi.Data;
using HealthcareApi.Middleware;
using HealthcareApi.Services;
using HealthcareApi.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using AutoMapper;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/healthcare-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Healthcare API application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();    // Add database context - MySQL
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

    // Add AutoMapper
    builder.Services.AddAutoMapper(typeof(HealthcareApi.Mappings.UserProfile).Assembly);

    // Add FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<UserRegistrationDtoValidator>();

    // Add Memory Cache
    builder.Services.AddMemoryCache();

    // Add Distributed Cache (Redis) - commented out for now, enable when Redis is available
    // builder.Services.AddStackExchangeRedisCache(options =>
    // {
    //     options.Configuration = builder.Configuration.GetConnectionString("Redis");
    // });    // Add Custom Services
    builder.Services.AddScoped<ICacheService, MemoryCacheService>(); // Use DistributedCacheService for Redis

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database")
        .AddCheck("memory", () =>
        {
            var allocated = GC.GetTotalMemory(false);
            var threshold = 1024L * 1024L * 1024L; // 1GB threshold
            return allocated < threshold ?
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Memory usage: {allocated / (1024 * 1024)} MB") :
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"High memory usage: {allocated / (1024 * 1024)} MB");
        })
        .AddCheck("disk-space", () =>
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            var hasLowSpace = drives.Any(d => d.AvailableFreeSpace < 1024L * 1024L * 1024L); // 1GB threshold
            return hasLowSpace ?
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Low disk space detected") :
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Sufficient disk space available");
        });

    // Add Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("Api", opt =>
        {
            opt.PermitLimit = 100; // 100 requests
            opt.Window = TimeSpan.FromMinutes(1); // per minute
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 10;
        });

        options.AddFixedWindowLimiter("Auth", opt =>
        {
            opt.PermitLimit = 5; // 5 requests
            opt.Window = TimeSpan.FromMinutes(1); // per minute
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
        });

        options.RejectionStatusCode = 429;
    });

    // Add services to the container
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            // Customize model validation response
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage);

                var result = new
                {
                    status = "BadRequest",
                    message = "Validation failed",
                    errors = errors,
                    timestamp = DateTime.UtcNow
                };

                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(result);
            };
        });

    builder.Services.AddEndpointsApiExplorer();

    // Enhanced Swagger configuration
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Healthcare API",
            Version = "v1",
            Description = "A comprehensive healthcare management API with JWT authentication",
            Contact = new OpenApiContact
            {
                Name = "Healthcare API Support",
                Email = "support@healthcare-api.com"
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });

        // Add JWT authentication to Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

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
        });    // Register services
    builder.Services.AddScoped<HealthcareApi.Services.IAuthService, HealthcareApi.Services.AuthService>();
    builder.Services.AddScoped<HealthcareApi.Services.IUserService, HealthcareApi.Services.UserService>();
    builder.Services.AddScoped<HealthcareApi.Services.IAppointmentService, HealthcareApi.Services.AppointmentService>();
    builder.Services.AddScoped<HealthcareApi.Services.IMessageService, HealthcareApi.Services.MessageService>();
    builder.Services.AddScoped<HealthcareApi.Services.IReminderService, HealthcareApi.Services.ReminderService>();
    builder.Services.AddScoped<HealthcareApi.Services.ITokenService, HealthcareApi.Services.TokenService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Add rate limiting
    app.UseRateLimiter();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Healthcare API V1");
            c.RoutePrefix = string.Empty; // Serve at root
        });
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

    // Add Health Check endpoints
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    exception = x.Value.Exception?.Message,
                    duration = x.Value.Duration.ToString(),
                    description = x.Value.Description
                }),
                totalDuration = report.TotalDuration
            };
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    });

    // Simple health check endpoint
    app.MapHealthChecks("/health/ready");

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
            var logger = services.GetRequiredService<ILogger<Program>>(); logger.LogError(ex, "An error occurred while initializing the database.");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
