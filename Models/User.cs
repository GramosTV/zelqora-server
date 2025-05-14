namespace HealthcareApi.Models;

public enum UserRole
{
    Patient,
    Doctor,
    Admin
}

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public UserRole Role { get; set; }
    public string? ProfilePicture { get; set; }
    public string? Specialization { get; set; } // For doctors
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Authentication properties
    public string PasswordHash { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Navigation properties
    public ICollection<Appointment>? DoctorAppointments { get; set; }
    public ICollection<Appointment>? PatientAppointments { get; set; }
    public ICollection<Message>? SentMessages { get; set; }
    public ICollection<Message>? ReceivedMessages { get; set; }
    public ICollection<Reminder>? Reminders { get; set; }
}
