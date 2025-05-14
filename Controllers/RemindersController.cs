using HealthcareApi.DTOs;
using HealthcareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthcareApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;
    private readonly ILogger<RemindersController> _logger;

    public RemindersController(IReminderService reminderService, ILogger<RemindersController> logger)
    {
        _reminderService = reminderService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllReminders()
    {
        try
        {
            var reminders = await _reminderService.GetAllRemindersAsync();
            return Ok(reminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all reminders");
            return StatusCode(500, new { message = "An error occurred while retrieving reminders" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReminder(string id)
    {
        try
        {
            var reminder = await _reminderService.GetReminderByIdAsync(id);

            if (reminder == null)
            {
                return NotFound(new { message = $"Reminder with ID {id} not found" });
            }

            // Check if the user has permission to view this reminder
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && reminder.UserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(reminder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reminder {ReminderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the reminder" });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetRemindersByUser(string userId)
    {
        try
        {
            // Check if the user has permission to view these reminders
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && userId != currentUserId)
            {
                return Forbid();
            }

            var reminders = await _reminderService.GetRemindersByUserIdAsync(userId);
            return Ok(reminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reminders for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving reminders" });
        }
    }

    [HttpGet("appointment/{appointmentId}")]
    public async Task<IActionResult> GetRemindersByAppointment(string appointmentId)
    {
        try
        {
            var reminders = await _reminderService.GetRemindersByAppointmentIdAsync(appointmentId);

            // Check if the user has permission to view these reminders
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin")
            {
                // Filter reminders for the current user only
                reminders = reminders.Where(r => r.UserId == currentUserId).ToList();
            }

            return Ok(reminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reminders for appointment {AppointmentId}", appointmentId);
            return StatusCode(500, new { message = "An error occurred while retrieving reminders" });
        }
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcomingReminders()
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var reminders = await _reminderService.GetUpcomingRemindersAsync(currentUserId);
            return Ok(reminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming reminders");
            return StatusCode(500, new { message = "An error occurred while retrieving upcoming reminders" });
        }
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadReminders()
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var reminders = await _reminderService.GetUnreadRemindersAsync(currentUserId);
            return Ok(reminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread reminders");
            return StatusCode(500, new { message = "An error occurred while retrieving unread reminders" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateReminder(CreateReminderDto reminderDto)
    {
        try
        {
            // Only allow creating reminders for the current user or if admin
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && reminderDto.UserId != currentUserId)
            {
                return Forbid();
            }

            var reminder = await _reminderService.CreateReminderAsync(reminderDto);
            return CreatedAtAction(nameof(GetReminder), new { id = reminder.Id }, reminder);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reminder");
            return StatusCode(500, new { message = "An error occurred while creating the reminder" });
        }
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        try
        {
            // Check if the reminder exists
            var existingReminder = await _reminderService.GetReminderByIdAsync(id);
            if (existingReminder == null)
            {
                return NotFound(new { message = $"Reminder with ID {id} not found" });
            }

            // Check if the user has permission to mark this reminder as read
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && existingReminder.UserId != currentUserId)
            {
                return Forbid();
            }

            var reminder = await _reminderService.MarkAsReadAsync(id);
            return Ok(reminder);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking reminder {ReminderId} as read", id);
            return StatusCode(500, new { message = "An error occurred while marking the reminder as read" });
        }
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReminder(string id)
    {
        try
        {
            // Check if the reminder exists
            var existingReminder = await _reminderService.GetReminderByIdAsync(id);
            if (existingReminder == null)
            {
                return NotFound(new { message = $"Reminder with ID {id} not found" });
            }

            // Check if the user has permission to delete this reminder
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && existingReminder.UserId != currentUserId)
            {
                return Forbid();
            }

            var result = await _reminderService.DeleteReminderAsync(id);

            if (!result)
            {
                return NotFound(new { message = $"Reminder with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reminder {ReminderId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the reminder" });
        }
    }

    [HttpPatch("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var count = await _reminderService.MarkAllAsReadAsync(currentUserId);
            return Ok(new { message = $"Marked {count} reminders as read", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all reminders as read");
            return StatusCode(500, new { message = "An error occurred while marking reminders as read" });
        }
    }
}
