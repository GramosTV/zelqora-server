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

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;

    public MessageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MessageDto>> GetConversation(string userId1, string userId2)
    {
        var conversation = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m =>
                (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return conversation.Select(MapToDto).ToList();
    }

    public async Task<List<MessageDto>> GetUserMessagesAsync(string userId)
    {
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(MapToDto).ToList();
    }

    public async Task<List<MessageDto>> GetUnreadMessagesAsync(string userId)
    {
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.ReceiverId == userId && !m.Read)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(MapToDto).ToList();
    }

    public async Task<MessageDto> SendMessageAsync(string senderId, CreateMessageDto messageDto)
    {
        // Validate sender exists
        var sender = await _context.Users.FindAsync(senderId);
        if (sender == null)
        {
            throw new KeyNotFoundException($"Sender with ID {senderId} not found");
        }

        // Validate receiver exists
        var receiver = await _context.Users.FindAsync(messageDto.ReceiverId);
        if (receiver == null)
        {
            throw new KeyNotFoundException($"Receiver with ID {messageDto.ReceiverId} not found");
        }

        // Create message
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = messageDto.ReceiverId,
            Content = messageDto.Content,
            Encrypted = messageDto.Encrypted,
            IntegrityHash = messageDto.IntegrityHash,
            Read = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Reload with related entities
        message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.Id == message.Id);

        return MapToDto(message!);
    }

    public async Task<MessageDto> MarkAsReadAsync(string messageId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null)
        {
            throw new KeyNotFoundException($"Message with ID {messageId} not found");
        }

        message.Read = true;
        message.UpdatedAt = DateTime.UtcNow;

        _context.Messages.Update(message);
        await _context.SaveChangesAsync();

        return MapToDto(message);
    }

    public async Task<bool> DeleteMessageAsync(string messageId)
    {
        var message = await _context.Messages.FindAsync(messageId);

        if (message == null)
        {
            return false;
        }

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();

        return true;
    }

    private static MessageDto MapToDto(Message message)
    {
        return new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            Content = message.Content,
            Encrypted = message.Encrypted,
            IntegrityHash = message.IntegrityHash,
            Read = message.Read,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            Sender = message.Sender != null
                ? new UserDto
                {
                    Id = message.Sender.Id,
                    Email = message.Sender.Email,
                    FirstName = message.Sender.FirstName,
                    LastName = message.Sender.LastName,
                    Role = message.Sender.Role.ToString(),
                    ProfilePicture = message.Sender.ProfilePicture,
                    Specialization = message.Sender.Specialization,
                    CreatedAt = message.Sender.CreatedAt,
                    UpdatedAt = message.Sender.UpdatedAt
                }
                : null,
            Receiver = message.Receiver != null
                ? new UserDto
                {
                    Id = message.Receiver.Id,
                    Email = message.Receiver.Email,
                    FirstName = message.Receiver.FirstName,
                    LastName = message.Receiver.LastName,
                    Role = message.Receiver.Role.ToString(),
                    ProfilePicture = message.Receiver.ProfilePicture,
                    Specialization = message.Receiver.Specialization,
                    CreatedAt = message.Receiver.CreatedAt,
                    UpdatedAt = message.Receiver.UpdatedAt
                }
                : null
        };
    }
}
