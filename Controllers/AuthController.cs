using HealthcareApi.DTOs;
using HealthcareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthcareApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(UserRegistrationDto registrationDto)
    {
        try
        {
            var result = await _authService.Register(registrationDto);
            return Ok(result);
        }
        catch (ApplicationException ex)
        {
            _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(UserLoginDto loginDto)
    {
        try
        {
            var result = await _authService.Login(loginDto);
            return Ok(result);
        }
        catch (ApplicationException ex)
        {
            _logger.LogWarning(ex, "Login failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var result = await _authService.RefreshToken(refreshTokenDto.RefreshToken);
            return Ok(result);
        }
        catch (ApplicationException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            await _authService.ForgotPassword(forgotPasswordDto.Email);
            // Always return OK to prevent email enumeration attacks
            return Ok(new { message = "If the email exists, a reset link has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password");
            // Still return OK to prevent email enumeration
            return Ok(new { message = "If the email exists, a reset link has been sent" });
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var result = await _authService.ResetPassword(resetPasswordDto);
            return Ok(new { message = "Password has been reset successfully" });
        }
        catch (ApplicationException ex)
        {
            _logger.LogWarning(ex, "Password reset failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new { message = "An error occurred during password reset" });
        }
    }
}
