namespace HealthcareApi.DTOs;

public class ReminderDto
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string AppointmentId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime ReminderDate { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AppointmentDto? Appointment { get; set; }
    public UserDto? User { get; set; }
}

public class CreateReminderDto
{
    public string UserId { get; set; } = null!;
    public string AppointmentId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime ReminderDate { get; set; }
}

public class UpdateReminderDto
{
    public bool IsRead { get; set; }
}
