using HealthcareApi.DTOs;
using HealthcareApi.Services;
using HealthcareApi.Validators;
using HealthcareApi.Helpers;
using HealthcareApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using FluentValidation;

namespace HealthcareApi.Controllers;

/// <summary>
/// Controller for managing user-related operations in the healthcare system.
/// Provides endpoints for user management, profile updates, and user search functionality.
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("Api")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly IValidator<UserRegistrationDto> _registrationValidator;
    private readonly IValidator<UserUpdateDto> _updateValidator;

    /// <summary>
    /// Initializes a new instance of the UsersController.
    /// </summary>
    /// <param name="userService">Service for user operations</param>
    /// <param name="logger">Logger for tracking operations and errors</param>
    /// <param name="registrationValidator">Validator for user registration</param>
    /// <param name="updateValidator">Validator for user updates</param>
    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger,
        IValidator<UserRegistrationDto> registrationValidator,
        IValidator<UserUpdateDto> updateValidator)
    {
        _userService = userService;
        _logger = logger;
        _registrationValidator = registrationValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Retrieves all users in the system.
    /// </summary>
    /// <returns>A list of all users with their basic information</returns>
    /// <response code="200">Returns the list of all users</response>
    /// <response code="401">If the user is unauthorized</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Retrieves a specific user by their ID.
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <returns>The user details if found</returns>
    /// <response code="200">Returns the user details</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="401">If the user is unauthorized</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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
    }    /// <summary>
         /// Retrieves all doctors in the system.
         /// </summary>
         /// <returns>A list of users with doctor role</returns>
         /// <response code="200">Returns the list of doctors</response>
         /// <response code="401">If the user is unauthorized</response>
         /// <response code="500">If an internal server error occurs</response>
    [HttpGet("doctors")]
    [ProducesResponseType(typeof(List<UserDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Retrieves a specialized list of doctors with additional doctor-specific information.
    /// </summary>
    /// <returns>A list of doctors with detailed doctor information</returns>
    /// <response code="200">Returns the specialized doctor list</response>
    /// <response code="401">If the user is unauthorized</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("doctors-list")]
    [ProducesResponseType(typeof(List<DoctorDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDoctorList()
    {
        try
        {
            var doctors = await _userService.GetDoctorListAsync();
            return Ok(doctors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor list");
            return StatusCode(500, new { message = "An error occurred while retrieving doctor list" });
        }
    }

    /// <summary>
    /// Retrieves all patients in the system.
    /// </summary>
    /// <returns>A list of users with patient role</returns>
    /// <response code="200">Returns the list of patients</response>
    /// <response code="401">If the user is unauthorized</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("patients")]
    [ProducesResponseType(typeof(List<UserDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Updates an existing user's information.
    /// Users can only update their own profile unless they are administrators.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update</param>
    /// <param name="updateDto">The user update information</param>
    /// <returns>The updated user information</returns>
    /// <response code="200">Returns the updated user</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="401">If the user is unauthorized</response>
    /// <response code="403">If the user lacks permission to update this profile</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateUser(string id, UserUpdateDto updateDto)
    {
        try
        {
            // Validate the update data
            var validationResult = await _updateValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })
                });
            }

            // Check if the user is updating their own profile or is an admin
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = RoleHelper.IsAdmin(User);

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
    }    /// <summary>
         /// Creates a new user in the system. Only administrators can create new users.
         /// </summary>
         /// <param name="userDto">The user registration information</param>
         /// <returns>The created user information</returns>
         /// <response code="201">Returns the newly created user</response>
         /// <response code="400">If the registration data is invalid</response>
         /// <response code="401">If the user is unauthorized</response>
         /// <response code="403">If the user lacks administrator permissions</response>
         /// <response code="500">If an internal server error occurs</response>    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(UserDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateUser(UserRegistrationDto userDto)
    {
        try
        {
            // Validate the registration data
            var validationResult = await _registrationValidator.ValidateAsync(userDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })
                });
            }

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

    /// <summary>
    /// Deletes a user from the system. Only administrators can delete users.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the user was successfully deleted</response>
    /// <response code="401">If the user is unauthorized</response>
    /// <response code="403">If the user lacks administrator permissions</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal server error occurs</response>    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Updates a user's profile picture.
    /// Users can only update their own profile picture unless they are administrators.
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <param name="pictureUrl">The URL of the new profile picture</param>
    /// <returns>The updated user information</returns>
    /// <response code="200">Returns the updated user</response>
    /// <response code="401">If the user is unauthorized</response>
    /// <response code="403">If the user lacks permission to update this profile</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPatch("{id}/profile-picture")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateProfilePicture(string id, [FromBody] string pictureUrl)
    {
        try
        {
            // Check if the user is updating their own profile or is an admin
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = RoleHelper.IsAdmin(User);

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

    /// <summary>
    /// Searches for users based on a query string.
    /// Searches across user names, email, and other relevant fields.
    /// </summary>
    /// <param name="query">The search query string</param>
    /// <returns>A list of users matching the search criteria</returns>
    /// <response code="200">Returns the list of matching users</response>
    /// <response code="401">If the user is unauthorized</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<UserDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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
