using HealthcareApi.Models;

namespace HealthcareApi.DTOs;

public class AppointmentDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public UserDto? Patient { get; set; }
    public UserDto? Doctor { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAppointmentDto
{
    public string Title { get; set; } = null!;
    public string PatientId { get; set; } = null!;
    public string DoctorId { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
}

public class UpdateAppointmentDto
{
    public string? Title { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public AppointmentStatus? Status { get; set; }
    public string? Notes { get; set; }
}
