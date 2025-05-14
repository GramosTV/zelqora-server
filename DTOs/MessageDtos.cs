namespace HealthcareApi.DTOs;

public class MessageDto
{
    public string Id { get; set; } = null!;
    public string SenderId { get; set; } = null!;
    public string ReceiverId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool Encrypted { get; set; }
    public string? IntegrityHash { get; set; }
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserDto? Sender { get; set; }
    public UserDto? Receiver { get; set; }
}

public class CreateMessageDto
{
    public string ReceiverId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool Encrypted { get; set; }
    public string? IntegrityHash { get; set; }
}

public class UpdateMessageDto
{
    public bool Read { get; set; }
}
