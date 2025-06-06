using AutoMapper;
using HealthcareApi.Data;
using HealthcareApi.DTOs;
using HealthcareApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthcareApi.Services;

public interface IMessageService
{
    Task<List<MessageDto>> GetConversation(string userId1, string userId2);
    Task<List<MessageDto>> GetUserMessagesAsync(string userId);
    Task<List<MessageDto>> GetUnreadMessagesAsync(string userId);
    Task<MessageDto> SendMessageAsync(string senderId, CreateMessageDto messageDto);
    Task<MessageDto> MarkAsReadAsync(string messageId);
    Task<bool> DeleteMessageAsync(string messageId);
}

/// <summary>
/// Enhanced message service with caching, AutoMapper, and comprehensive error handling
/// </summary>
public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MessageService> _logger;

    private const int CACHE_EXPIRY_MINUTES = 5; // Messages are time-sensitive

    public MessageService(
        ApplicationDbContext context,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<MessageService> logger)
    {
        _context = context;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
    }
    public async Task<List<MessageDto>> GetConversation(string userId1, string userId2)
    {
        try
        {
            _logger.LogInformation("Retrieving conversation between users {UserId1} and {UserId2}", userId1, userId2);

            // Create unique cache key for conversation
            var cacheKey = $"conversation_{(string.Compare(userId1, userId2) < 0 ? userId1 + "_" + userId2 : userId2 + "_" + userId1)}";

            // Check cache first
            var cachedConversation = await _cacheService.GetAsync<List<MessageDto>>(cacheKey);
            if (cachedConversation != null)
            {
                _logger.LogDebug("Retrieved conversation from cache with {Count} messages", cachedConversation.Count);
                return cachedConversation;
            }

            var conversation = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                    (m.SenderId == userId2 && m.ReceiverId == userId1))
                .OrderBy(m => m.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var conversationDtos = _mapper.Map<List<MessageDto>>(conversation);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, conversationDtos, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogInformation("Retrieved conversation with {Count} messages from database", conversationDtos.Count);
            return conversationDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation between users {UserId1} and {UserId2}", userId1, userId2);
            throw;
        }
    }
    public async Task<List<MessageDto>> GetUserMessagesAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Retrieving all messages for user {UserId}", userId);

            var cacheKey = $"user_messages_{userId}";

            // Check cache first
            var cachedMessages = await _cacheService.GetAsync<List<MessageDto>>(cacheKey);
            if (cachedMessages != null)
            {
                _logger.LogDebug("Retrieved {Count} user messages from cache", cachedMessages.Count);
                return cachedMessages;
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var messageDtos = _mapper.Map<List<MessageDto>>(messages);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, messageDtos, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogInformation("Retrieved {Count} messages for user from database", messageDtos.Count);
            return messageDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for user {UserId}", userId);
            throw;
        }
    }
    public async Task<List<MessageDto>> GetUnreadMessagesAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Retrieving unread messages for user {UserId}", userId);

            var cacheKey = $"unread_messages_{userId}";

            // Check cache first
            var cachedMessages = await _cacheService.GetAsync<List<MessageDto>>(cacheKey);
            if (cachedMessages != null)
            {
                _logger.LogDebug("Retrieved {Count} unread messages from cache", cachedMessages.Count);
                return cachedMessages;
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == userId && !m.Read)
                .OrderByDescending(m => m.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var messageDtos = _mapper.Map<List<MessageDto>>(messages);

            // Cache with shorter expiry for unread messages (very time-sensitive)
            await _cacheService.SetAsync(cacheKey, messageDtos, TimeSpan.FromMinutes(2));

            _logger.LogInformation("Retrieved {Count} unread messages for user from database", messageDtos.Count);
            return messageDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread messages for user {UserId}", userId);
            throw;
        }
    }
    public async Task<MessageDto> SendMessageAsync(string senderId, CreateMessageDto messageDto)
    {
        try
        {
            _logger.LogInformation("Sending message from {SenderId} to {ReceiverId}", senderId, messageDto.ReceiverId);

            // Validate sender exists
            var sender = await _context.Users.FindAsync(senderId);
            if (sender == null)
            {
                _logger.LogWarning("Sender not found: {SenderId}", senderId);
                throw new KeyNotFoundException($"Sender with ID {senderId} not found");
            }

            // Validate receiver exists
            var receiver = await _context.Users.FindAsync(messageDto.ReceiverId);
            if (receiver == null)
            {
                _logger.LogWarning("Receiver not found: {ReceiverId}", messageDto.ReceiverId);
                throw new KeyNotFoundException($"Receiver with ID {messageDto.ReceiverId} not found");
            }

            // Create message using AutoMapper
            var message = _mapper.Map<Message>(messageDto);
            message.SenderId = senderId;
            message.Read = false;

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Reload with related entities
            message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.Id == message.Id);

            // Invalidate related caches
            await InvalidateMessageCaches(senderId, messageDto.ReceiverId);

            var messageReturnDto = _mapper.Map<MessageDto>(message);

            _logger.LogInformation("Successfully sent message {MessageId} from {SenderId} to {ReceiverId}",
                message!.Id, senderId, messageDto.ReceiverId);

            return messageReturnDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message from {SenderId} to {ReceiverId}", senderId, messageDto.ReceiverId);
            throw;
        }
    }
    public async Task<MessageDto> MarkAsReadAsync(string messageId)
    {
        try
        {
            _logger.LogInformation("Marking message as read: {MessageId}", messageId);

            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                _logger.LogWarning("Message not found: {MessageId}", messageId);
                throw new KeyNotFoundException($"Message with ID {messageId} not found");
            }

            if (message.Read)
            {
                _logger.LogDebug("Message {MessageId} already marked as read", messageId);
                return _mapper.Map<MessageDto>(message);
            }

            message.Read = true;
            message.UpdatedAt = DateTime.UtcNow;

            _context.Messages.Update(message);
            await _context.SaveChangesAsync();

            // Invalidate related caches
            await InvalidateMessageCaches(message.SenderId, message.ReceiverId);

            var messageDto = _mapper.Map<MessageDto>(message);

            _logger.LogInformation("Successfully marked message {MessageId} as read", messageId);
            return messageDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read: {MessageId}", messageId);
            throw;
        }
    }
    public async Task<bool> DeleteMessageAsync(string messageId)
    {
        try
        {
            _logger.LogInformation("Deleting message: {MessageId}", messageId);

            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
            {
                _logger.LogWarning("Message not found for deletion: {MessageId}", messageId);
                return false;
            }

            var senderId = message.SenderId;
            var receiverId = message.ReceiverId;

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            // Invalidate related caches
            await InvalidateMessageCaches(senderId, receiverId);

            _logger.LogInformation("Successfully deleted message: {MessageId}", messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message: {MessageId}", messageId);
            throw;
        }
    }

    private async Task InvalidateMessageCaches(string senderId, string receiverId)
    {
        // Clear user-specific caches
        await _cacheService.RemoveAsync($"user_messages_{senderId}");
        await _cacheService.RemoveAsync($"user_messages_{receiverId}");

        // Clear unread message caches
        await _cacheService.RemoveAsync($"unread_messages_{senderId}");
        await _cacheService.RemoveAsync($"unread_messages_{receiverId}");

        // Clear conversation cache
        var conversationKey = $"conversation_{(string.Compare(senderId, receiverId) < 0 ? senderId + "_" + receiverId : receiverId + "_" + senderId)}";
        await _cacheService.RemoveAsync(conversationKey);
    }
}
