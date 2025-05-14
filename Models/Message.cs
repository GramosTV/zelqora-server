namespace HealthcareApi.Models;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SenderId { get; set; } = null!;
    public string ReceiverId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool Encrypted { get; set; }
    public string? IntegrityHash { get; set; }
    public bool Read { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? Sender { get; set; }
    public User? Receiver { get; set; }
}
