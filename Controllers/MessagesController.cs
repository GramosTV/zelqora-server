using HealthcareApi.DTOs;
using HealthcareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthcareApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    [HttpGet("conversation/{otherUserId}")]
    public async Task<IActionResult> GetConversation(string otherUserId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var conversation = await _messageService.GetConversation(currentUserId, otherUserId);
            return Ok(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation with user {OtherUserId}", otherUserId);
            return StatusCode(500, new { message = "An error occurred while retrieving the conversation" });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserMessages(string userId)
    {
        try
        {
            // Check if the user has permission to view these messages
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && userId != currentUserId)
            {
                return Forbid();
            }

            var messages = await _messageService.GetUserMessagesAsync(userId);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving messages" });
        }
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadMessages()
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var unreadMessages = await _messageService.GetUnreadMessagesAsync(currentUserId);
            return Ok(unreadMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread messages");
            return StatusCode(500, new { message = "An error occurred while retrieving unread messages" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(CreateMessageDto messageDto)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var message = await _messageService.SendMessageAsync(currentUserId, messageDto);
            return CreatedAtAction(nameof(GetUserMessages), new { userId = currentUserId }, message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new { message = "An error occurred while sending the message" });
        }
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        try
        {
            var message = await _messageService.MarkAsReadAsync(id);
            return Ok(message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as read", id);
            return StatusCode(500, new { message = "An error occurred while marking the message as read" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(string id)
    {
        try
        {
            var result = await _messageService.DeleteMessageAsync(id);

            if (!result)
            {
                return NotFound(new { message = $"Message with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the message" });
        }
    }
}
