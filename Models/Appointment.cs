namespace HealthcareApi.Models;

public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

public class Appointment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = null!;
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? Patient { get; set; }
    public User? Doctor { get; set; }
    public ICollection<Reminder>? Reminders { get; set; }
}
