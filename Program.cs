using System.Text;
using HealthcareApi.Data;
using HealthcareApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "HealthcareApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "HealthcareApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Key"] ?? "Q9GD4P3PuPzYw5nvRNAD7yBSqGjZqQHP6gYjvuVT8F6RRQyhxQ"))
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

// Create database if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database.");
    }
}

app.Run();
