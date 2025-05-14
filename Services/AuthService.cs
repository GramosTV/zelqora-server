using HealthcareApi.Data;
using HealthcareApi.DTOs;
using HealthcareApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthcareApi.Services;

public interface IAuthService
{
    Task<AuthResponseDto> Register(UserRegistrationDto registrationDto);
    Task<AuthResponseDto> Login(UserLoginDto loginDto);
    Task<TokenResponseDto> RefreshToken(string refreshToken);
    Task<bool> ForgotPassword(string email);
    Task<bool> ResetPassword(ResetPasswordDto resetPasswordDto);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Register(UserRegistrationDto registrationDto)
    {
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == registrationDto.Email))
        {
            throw new ApplicationException("Email already exists");
        }

        // Create new user
        var user = new User
        {
            Email = registrationDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registrationDto.Password),
            FirstName = registrationDto.FirstName,
            LastName = registrationDto.LastName,
            Role = registrationDto.Role,
            Specialization = registrationDto.Specialization
        };

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        // Save user
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Return response
        return new AuthResponseDto
        {
            User = MapUserToDto(user),
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponseDto> Login(UserLoginDto loginDto)
    {
        // Find user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        // Check if user exists and password is correct
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new ApplicationException("Invalid email or password");
        }

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        // Save user
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        // Return response
        return new AuthResponseDto
        {
            User = MapUserToDto(user),
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<TokenResponseDto> RefreshToken(string refreshToken)
    {
        // Find user by refresh token
        var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        // Check if user exists and token is valid
        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            throw new ApplicationException("Invalid or expired refresh token");
        }

        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        // Save user
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        // Return response
        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken
        };
    }

    public async Task<bool> ForgotPassword(string email)
    {
        // Find user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            // Don't reveal that the user doesn't exist for security
            return true;
        }

        // In a real application, you would generate a password reset token,
        // store it, and send it to the user's email address
        // For this demo, we'll just return true

        _logger.LogInformation($"Password reset requested for {email}");
        return true;
    }
    public async Task<bool> ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        // In a real application, you would validate the token and find the user
        // For this demo, we'll just return true

        _logger.LogInformation("Password reset attempted");

        // Add an await to make the method truly async
        await Task.CompletedTask;
        return true;
    }

    private UserDto MapUserToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            ProfilePicture = user.ProfilePicture,
            Specialization = user.Specialization,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
