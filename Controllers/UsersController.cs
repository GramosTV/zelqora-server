using HealthcareApi.DTOs;
using HealthcareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthcareApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the user" });
        }
    }

    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctors()
    {
        try
        {
            var doctors = await _userService.GetDoctorsAsync();
            return Ok(doctors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors");
            return StatusCode(500, new { message = "An error occurred while retrieving doctors" });
        }
    }

    [HttpGet("patients")]
    public async Task<IActionResult> GetPatients()
    {
        try
        {
            var patients = await _userService.GetPatientsAsync();
            return Ok(patients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patients");
            return StatusCode(500, new { message = "An error occurred while retrieving patients" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UserUpdateDto updateDto)
    {
        try
        {
            // Check if the user is updating their own profile or is an admin
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (currentUserId != id && !isAdmin)
            {
                return Forbid();
            }

            var user = await _userService.UpdateUserAsync(id, updateDto);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ApplicationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the user" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(UserRegistrationDto userDto)
    {
        try
        {
            var user = await _userService.CreateUserAsync(userDto);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (ApplicationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(id);

            if (!result)
            {
                return NotFound(new { message = $"User with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the user" });
        }
    }

    [HttpPatch("{id}/profile-picture")]
    public async Task<IActionResult> UpdateProfilePicture(string id, [FromBody] string pictureUrl)
    {
        try
        {
            // Check if the user is updating their own profile or is an admin
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (currentUserId != id && !isAdmin)
            {
                return Forbid();
            }

            var user = await _userService.UpdateProfilePictureAsync(id, pictureUrl);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile picture for user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the profile picture" });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string query)
    {
        try
        {
            var users = await _userService.SearchUsersAsync(query);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with query {Query}", query);
            return StatusCode(500, new { message = "An error occurred while searching for users" });
        }
    }
}
