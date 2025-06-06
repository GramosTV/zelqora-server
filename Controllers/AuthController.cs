using HealthcareApi.DTOs;
using HealthcareApi.Services;
using HealthcareApi.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;

namespace HealthcareApi.Controllers;

/// <summary>
/// Controller for authentication-related operations.
/// Provides endpoints for user registration, login, and token management.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("Auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IValidator<UserRegistrationDto> _registrationValidator;
    private readonly IValidator<UserLoginDto> _loginValidator;

    /// <summary>
    /// Initializes a new instance of the AuthController.
    /// </summary>
    /// <param name="authService">Service for authentication operations</param>
    /// <param name="logger">Logger for tracking operations and errors</param>
    /// <param name="registrationValidator">Validator for user registration</param>
    /// <param name="loginValidator">Validator for user login</param>
    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger,
        IValidator<UserRegistrationDto> registrationValidator,
        IValidator<UserLoginDto> loginValidator)
    {
        _authService = authService;
        _logger = logger;
        _registrationValidator = registrationValidator;
        _loginValidator = loginValidator;
    }    /// <summary>
         /// Registers a new user in the system.
         /// </summary>
         /// <param name="registrationDto">User registration information</param>
         /// <returns>Authentication result with user details and token</returns>
         /// <response code="200">Returns the authentication result</response>
         /// <response code="400">If the registration data is invalid</response>
         /// <response code="429">If too many requests are made</response>
         /// <response code="500">If an internal server error occurs</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(429)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Register(UserRegistrationDto registrationDto)
    {
        try
        {
            // Validate the registration data
            var validationResult = await _registrationValidator.ValidateAsync(registrationDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })
                });
            }

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

    /// <summary>
    /// Authenticates a user and returns an access token.
    /// </summary>
    /// <param name="loginDto">User login credentials</param>
    /// <returns>Authentication result with user details and token</returns>
    /// <response code="200">Returns the authentication result</response>
    /// <response code="400">If the login credentials are invalid</response>
    /// <response code="401">If the credentials are incorrect</response>
    /// <response code="429">If too many requests are made</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(429)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Login(UserLoginDto loginDto)
    {
        try
        {
            // Validate the login data
            var validationResult = await _loginValidator.ValidateAsync(loginDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })
                });
            }

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
