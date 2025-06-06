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
/// Controller for managing message-related operations in the healthcare system.
/// Provides endpoints for sending, receiving, and managing messages between users.
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("Api")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ILogger<MessagesController> _logger;
    private readonly IValidator<CreateMessageDto> _createValidator;
    private readonly IValidator<UpdateMessageDto> _updateValidator;

    /// <summary>
    /// Initializes a new instance of the MessagesController.
    /// </summary>
    /// <param name="messageService">Service for message operations</param>
    /// <param name="logger">Logger for tracking operations and errors</param>
    /// <param name="createValidator">Validator for message creation</param>
    /// <param name="updateValidator">Validator for message updates</param>
    public MessagesController(
        IMessageService messageService,
        ILogger<MessagesController> logger,
        IValidator<CreateMessageDto> createValidator,
        IValidator<UpdateMessageDto> updateValidator)
    {
        _messageService = messageService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Retrieves all messages in a conversation between the current user and another user.
    /// Messages are returned in chronological order.
    /// </summary>
    /// <param name="otherUserId">The unique identifier of the other user in the conversation</param>
    /// <returns>List of messages between the two users</returns>
    /// <response code="200">Returns the conversation messages</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("conversation/{otherUserId}")]
    [ProducesResponseType<IEnumerable<MessageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Retrieves all messages for a specific user (sent and received).
    /// Users can only access their own messages unless they are administrators.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>List of all messages for the specified user</returns>
    /// <response code="200">Returns the user's messages</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to view these messages</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType<IEnumerable<MessageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserMessages(string userId)
    {
        try
        {
            // Check if the user has permission to view these messages
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!RoleHelper.CanAccessUserResource(User, userId))
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

    /// <summary>
    /// Retrieves all unread messages for the current user.
    /// Only returns messages where the current user is the recipient and the message is unread.
    /// </summary>
    /// <returns>List of unread messages for the current user</returns>
    /// <response code="200">Returns the unread messages</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("unread")]
    [ProducesResponseType<IEnumerable<MessageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Sends a new message to another user in the system.
    /// The sender is automatically set to the current authenticated user.
    /// </summary>
    /// <param name="messageDto">The message content and recipient information</param>
    /// <returns>The created message details</returns>
    /// <response code="201">Returns the newly created message</response>
    /// <response code="400">If the message data is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="404">If the recipient user is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpPost]
    [ProducesResponseType<MessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendMessage(CreateMessageDto messageDto)
    {
        try
        {
            // Validate the message data
            var validationResult = await _createValidator.ValidateAsync(messageDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })
                });
            }

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

    /// <summary>
    /// Marks a specific message as read by the current user.
    /// Only the recipient of the message can mark it as read.
    /// </summary>
    /// <param name="id">The unique identifier of the message to mark as read</param>
    /// <returns>The updated message details</returns>
    /// <response code="200">Returns the updated message</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user is not the recipient of the message</response>
    /// <response code="404">If the message is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpPatch("{id}/read")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Deletes a message from the system.
    /// Only the sender or recipient of the message (or admins) can delete it.
    /// </summary>
    /// <param name="id">The unique identifier of the message to delete</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the message was successfully deleted</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to delete this message</response>
    /// <response code="404">If the message is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
