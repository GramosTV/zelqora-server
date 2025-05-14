using HealthcareApi.Data;
using HealthcareApi.DTOs;
using HealthcareApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthcareApi.Services;

public interface IAppointmentService
{
    Task<List<AppointmentDto>> GetAllAppointmentsAsync();
    Task<AppointmentDto?> GetAppointmentByIdAsync(string id);
    Task<List<AppointmentDto>> GetAppointmentsByDoctorIdAsync(string doctorId);
    Task<List<AppointmentDto>> GetAppointmentsByPatientIdAsync(string patientId);
    Task<List<AppointmentDto>> GetUpcomingAppointmentsAsync();
    Task<List<AppointmentDto>> GetTodaysAppointmentsAsync();
    Task<List<AppointmentDto>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto appointmentDto);
    Task<AppointmentDto> UpdateAppointmentAsync(string id, UpdateAppointmentDto appointmentDto);
    Task<bool> DeleteAppointmentAsync(string id);
    Task<AppointmentDto> UpdateAppointmentStatusAsync(string id, AppointmentStatus status);
}

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;

    public AppointmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppointmentDto>> GetAllAppointmentsAsync()
    {
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<AppointmentDto?> GetAppointmentByIdAsync(string id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        return appointment != null ? MapToDto(appointment) : null;
    }

    public async Task<List<AppointmentDto>> GetAppointmentsByDoctorIdAsync(string doctorId)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.DoctorId == doctorId)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<AppointmentDto>> GetAppointmentsByPatientIdAsync(string patientId)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patientId)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<AppointmentDto>> GetUpcomingAppointmentsAsync()
    {
        var now = DateTime.UtcNow;
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.StartTime > now && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<AppointmentDto>> GetTodaysAppointmentsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.StartTime >= today && a.StartTime < tomorrow)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<AppointmentDto>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.StartTime >= startDate && a.StartTime <= endDate)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto appointmentDto)
    {
        // Validate that patient exists
        var patient = await _context.Users.FindAsync(appointmentDto.PatientId);
        if (patient == null)
        {
            throw new KeyNotFoundException($"Patient with ID {appointmentDto.PatientId} not found");
        }

        // Validate that doctor exists
        var doctor = await _context.Users.FindAsync(appointmentDto.DoctorId);
        if (doctor == null || doctor.Role != UserRole.Doctor)
        {
            throw new KeyNotFoundException($"Doctor with ID {appointmentDto.DoctorId} not found");
        }

        // Create appointment
        var appointment = new Appointment
        {
            Title = appointmentDto.Title,
            PatientId = appointmentDto.PatientId,
            DoctorId = appointmentDto.DoctorId,
            StartTime = appointmentDto.StartTime,
            EndTime = appointmentDto.EndTime,
            Status = appointmentDto.Status,
            Notes = appointmentDto.Notes
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Reload with related entities
        appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);

        return MapToDto(appointment!);
    }

    public async Task<AppointmentDto> UpdateAppointmentAsync(string id, UpdateAppointmentDto appointmentDto)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
        {
            throw new KeyNotFoundException($"Appointment with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(appointmentDto.Title))
            appointment.Title = appointmentDto.Title;

        if (appointmentDto.StartTime.HasValue)
            appointment.StartTime = appointmentDto.StartTime.Value;

        if (appointmentDto.EndTime.HasValue)
            appointment.EndTime = appointmentDto.EndTime.Value;

        if (appointmentDto.Status.HasValue)
            appointment.Status = appointmentDto.Status.Value;

        if (appointmentDto.Notes != null) // Allow setting notes to empty string
            appointment.Notes = appointmentDto.Notes;

        appointment.UpdatedAt = DateTime.UtcNow;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return MapToDto(appointment);
    }

    public async Task<bool> DeleteAppointmentAsync(string id)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
        {
            return false;
        }

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<AppointmentDto> UpdateAppointmentStatusAsync(string id, AppointmentStatus status)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
        {
            throw new KeyNotFoundException($"Appointment with ID {id} not found");
        }

        appointment.Status = status;
        appointment.UpdatedAt = DateTime.UtcNow;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return MapToDto(appointment);
    }

    private static AppointmentDto MapToDto(Appointment appointment)
    {
        return new AppointmentDto
        {
            Id = appointment.Id,
            Title = appointment.Title,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            Patient = appointment.Patient != null
                ? new UserDto
                {
                    Id = appointment.Patient.Id,
                    Email = appointment.Patient.Email,
                    FirstName = appointment.Patient.FirstName,
                    LastName = appointment.Patient.LastName,
                    Role = appointment.Patient.Role.ToString(),
                    ProfilePicture = appointment.Patient.ProfilePicture,
                    Specialization = appointment.Patient.Specialization,
                    CreatedAt = appointment.Patient.CreatedAt,
                    UpdatedAt = appointment.Patient.UpdatedAt
                }
                : null,
            Doctor = appointment.Doctor != null
                ? new UserDto
                {
                    Id = appointment.Doctor.Id,
                    Email = appointment.Doctor.Email,
                    FirstName = appointment.Doctor.FirstName,
                    LastName = appointment.Doctor.LastName,
                    Role = appointment.Doctor.Role.ToString(),
                    ProfilePicture = appointment.Doctor.ProfilePicture,
                    Specialization = appointment.Doctor.Specialization,
                    CreatedAt = appointment.Doctor.CreatedAt,
                    UpdatedAt = appointment.Doctor.UpdatedAt
                }
                : null,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status.ToString(),
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt
        };
    }
}
