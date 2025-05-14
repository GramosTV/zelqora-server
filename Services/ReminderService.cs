using HealthcareApi.Data;
using HealthcareApi.DTOs;
using HealthcareApi.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace HealthcareApi.Services;

public interface IReminderService
{
    Task<List<ReminderDto>> GetAllRemindersAsync();
    Task<ReminderDto?> GetReminderByIdAsync(string id);
    Task<List<ReminderDto>> GetRemindersByUserIdAsync(string userId);
    Task<List<ReminderDto>> GetRemindersByAppointmentIdAsync(string appointmentId);
    Task<List<ReminderDto>> GetUpcomingRemindersAsync(string userId);
    Task<List<ReminderDto>> GetUnreadRemindersAsync(string userId);
    Task<ReminderDto> CreateReminderAsync(CreateReminderDto reminderDto);
    Task<ReminderDto> MarkAsReadAsync(string id);
    Task<int> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteReminderAsync(string id);
}

public class ReminderService : IReminderService
{
    private readonly ApplicationDbContext _context;

    public ReminderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReminderDto>> GetAllRemindersAsync()
    {
        var reminders = await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient)
            .ToListAsync();

        return reminders.Select(MapToDto).ToList();
    }

    public async Task<ReminderDto?> GetReminderByIdAsync(string id)
    {
        var reminder = await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(r => r.Id == id);

        return reminder != null ? MapToDto(reminder) : null;
    }

    public async Task<List<ReminderDto>> GetRemindersByUserIdAsync(string userId)
    {
        var reminders = await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReminderDate)
            .ToListAsync();

        return reminders.Select(MapToDto).ToList();
    }

    public async Task<List<ReminderDto>> GetRemindersByAppointmentIdAsync(string appointmentId)
    {
        var reminders = await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient)
            .Where(r => r.AppointmentId == appointmentId)
            .OrderByDescending(r => r.ReminderDate)
            .ToListAsync();

        return reminders.Select(MapToDto).ToList();
    }

    public async Task<List<ReminderDto>> GetUpcomingRemindersAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var reminders = await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient)
            .Where(r => r.UserId == userId && r.ReminderDate > now)
            .OrderBy(r => r.ReminderDate)
            .ToListAsync();

        return reminders.Select(MapToDto).ToList();
    }

    public async Task<List<ReminderDto>> GetUnreadRemindersAsync(string userId)
    {
        var reminders = await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient)
            .Where(r => r.UserId == userId && !r.IsRead)
            .OrderByDescending(r => r.ReminderDate)
            .ToListAsync();

        return reminders.Select(MapToDto).ToList();
    }

    public async Task<ReminderDto> CreateReminderAsync(CreateReminderDto reminderDto)
    {
        // Validate that user exists
        var user = await _context.Users.FindAsync(reminderDto.UserId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {reminderDto.UserId} not found");
        }

        // Validate that appointment exists
        var appointment = await _context.Appointments.FindAsync(reminderDto.AppointmentId);
        if (appointment == null)
        {
            throw new KeyNotFoundException($"Appointment with ID {reminderDto.AppointmentId} not found");
        }

        // Create reminder
        var reminder = new Reminder
        {
            UserId = reminderDto.UserId,
            AppointmentId = reminderDto.AppointmentId,
            Title = reminderDto.Title,
            Message = reminderDto.Message,
            ReminderDate = reminderDto.ReminderDate,
            IsRead = false
        };

        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();        // Reload with related entities
        var createdReminder = await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(r => r.Id == reminder.Id);

        if (createdReminder == null)
        {
            throw new InvalidOperationException($"Failed to retrieve created reminder with ID {reminder.Id}");
        }

        return MapToDto(createdReminder);
    }

    public async Task<ReminderDto> MarkAsReadAsync(string id)
    {
        var reminder = await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reminder == null)
        {
            throw new KeyNotFoundException($"Reminder with ID {id} not found");
        }

        reminder.IsRead = true;
        reminder.UpdatedAt = DateTime.UtcNow;

        _context.Reminders.Update(reminder);
        await _context.SaveChangesAsync();

        return MapToDto(reminder);
    }
    public async Task<bool> DeleteReminderAsync(string id)
    {
        var reminder = await _context.Reminders.FindAsync(id);

        if (reminder == null)
        {
            return false;
        }

        _context.Reminders.Remove(reminder);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(string userId)
    {
        var unreadReminders = await _context.Reminders
            .Where(r => r.UserId == userId && !r.IsRead)
            .ToListAsync();

        if (!unreadReminders.Any())
        {
            return 0;
        }

        foreach (var reminder in unreadReminders)
        {
            reminder.IsRead = true;
            reminder.UpdatedAt = DateTime.UtcNow;
        }

        _context.Reminders.UpdateRange(unreadReminders);
        await _context.SaveChangesAsync();

        return unreadReminders.Count;
    }
    private static ReminderDto MapToDto(Reminder reminder)
    {
        var reminderDto = new ReminderDto
        {
            Id = reminder.Id,
            UserId = reminder.UserId,
            AppointmentId = reminder.AppointmentId,
            Title = reminder.Title,
            Message = reminder.Message,
            ReminderDate = reminder.ReminderDate,
            IsRead = reminder.IsRead,
            CreatedAt = reminder.CreatedAt,
            UpdatedAt = reminder.UpdatedAt,
            User = null,
            Appointment = null
        };

        // Safely map User if not null
        if (reminder.User != null)
        {
            reminderDto.User = new UserDto
            {
                Id = reminder.User.Id,
                Email = reminder.User.Email,
                FirstName = reminder.User.FirstName,
                LastName = reminder.User.LastName,
                Role = reminder.User.Role.ToString(),
                ProfilePicture = reminder.User.ProfilePicture,
                Specialization = reminder.User.Specialization,
                CreatedAt = reminder.User.CreatedAt,
                UpdatedAt = reminder.User.UpdatedAt
            };
        }

        // Safely map Appointment if not null
        if (reminder.Appointment != null)
        {
            var appointmentDto = new AppointmentDto
            {
                Id = reminder.Appointment.Id,
                Title = reminder.Appointment.Title,
                PatientId = reminder.Appointment.PatientId,
                DoctorId = reminder.Appointment.DoctorId,
                StartTime = reminder.Appointment.StartTime,
                EndTime = reminder.Appointment.EndTime,
                Status = reminder.Appointment.Status.ToString(),
                Notes = reminder.Appointment.Notes,
                CreatedAt = reminder.Appointment.CreatedAt,
                UpdatedAt = reminder.Appointment.UpdatedAt,
                Patient = null,
                Doctor = null
            };

            // Safely map Patient if not null
            if (reminder.Appointment.Patient != null)
            {
                appointmentDto.Patient = new UserDto
                {
                    Id = reminder.Appointment.Patient.Id,
                    Email = reminder.Appointment.Patient.Email,
                    FirstName = reminder.Appointment.Patient.FirstName,
                    LastName = reminder.Appointment.Patient.LastName,
                    Role = reminder.Appointment.Patient.Role.ToString(),
                    ProfilePicture = reminder.Appointment.Patient.ProfilePicture,
                    CreatedAt = reminder.Appointment.Patient.CreatedAt,
                    UpdatedAt = reminder.Appointment.Patient.UpdatedAt
                };
            }

            // Safely map Doctor if not null
            if (reminder.Appointment.Doctor != null)
            {
                appointmentDto.Doctor = new UserDto
                {
                    Id = reminder.Appointment.Doctor.Id,
                    Email = reminder.Appointment.Doctor.Email,
                    FirstName = reminder.Appointment.Doctor.FirstName,
                    LastName = reminder.Appointment.Doctor.LastName,
                    Role = reminder.Appointment.Doctor.Role.ToString(),
                    ProfilePicture = reminder.Appointment.Doctor.ProfilePicture,
                    Specialization = reminder.Appointment.Doctor.Specialization,
                    CreatedAt = reminder.Appointment.Doctor.CreatedAt,
                    UpdatedAt = reminder.Appointment.Doctor.UpdatedAt
                };
            }

            reminderDto.Appointment = appointmentDto;
        }

        return reminderDto;
    }
}
