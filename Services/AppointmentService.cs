using AutoMapper;
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

/// <summary>
/// Enhanced appointment service with caching, AutoMapper, and comprehensive error handling
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AppointmentService> _logger;

    private const string ALL_APPOINTMENTS_CACHE_KEY = "all_appointments";
    private const string UPCOMING_APPOINTMENTS_CACHE_KEY = "upcoming_appointments";
    private const string TODAY_APPOINTMENTS_CACHE_KEY = "today_appointments";
    private const int CACHE_EXPIRY_MINUTES = 15; // Shorter cache for appointments

    public AppointmentService(
        ApplicationDbContext context,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<AppointmentService> logger)
    {
        _context = context;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
    }
    public async Task<List<AppointmentDto>> GetAllAppointmentsAsync()
    {
        try
        {
            // Check cache first
            var cachedAppointments = await _cacheService.GetAsync<List<AppointmentDto>>(ALL_APPOINTMENTS_CACHE_KEY);
            if (cachedAppointments != null)
            {
                _logger.LogDebug("Retrieved {Count} appointments from cache", cachedAppointments.Count);
                return cachedAppointments;
            }

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsNoTracking()
                .ToListAsync();

            var appointmentDtos = _mapper.Map<List<AppointmentDto>>(appointments);

            // Cache the result
            await _cacheService.SetAsync(ALL_APPOINTMENTS_CACHE_KEY, appointmentDtos, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogInformation("Retrieved {Count} appointments from database", appointmentDtos.Count);
            return appointmentDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all appointments");
            throw;
        }
    }

    public async Task<AppointmentDto?> GetAppointmentByIdAsync(string id)
    {
        try
        {
            var cacheKey = $"appointment_{id}";

            // Check cache first
            var cachedAppointment = await _cacheService.GetAsync<AppointmentDto>(cacheKey);
            if (cachedAppointment != null)
            {
                _logger.LogDebug("Retrieved appointment {AppointmentId} from cache", id);
                return cachedAppointment;
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                _logger.LogWarning("Appointment with ID {AppointmentId} not found", id);
                return null;
            }

            var appointmentDto = _mapper.Map<AppointmentDto>(appointment);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, appointmentDto, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogDebug("Retrieved appointment {AppointmentId} from database", id);
            return appointmentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointment {AppointmentId}", id);
            throw;
        }
    }
    public async Task<List<AppointmentDto>> GetAppointmentsByDoctorIdAsync(string doctorId)
    {
        try
        {
            var cacheKey = $"appointments_doctor_{doctorId}";

            // Check cache first
            var cachedAppointments = await _cacheService.GetAsync<List<AppointmentDto>>(cacheKey);
            if (cachedAppointments != null)
            {
                _logger.LogDebug("Retrieved {Count} appointments for doctor {DoctorId} from cache", cachedAppointments.Count, doctorId);
                return cachedAppointments;
            }

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.DoctorId == doctorId)
                .AsNoTracking()
                .ToListAsync();

            var appointmentDtos = _mapper.Map<List<AppointmentDto>>(appointments);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, appointmentDtos, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogInformation("Retrieved {Count} appointments for doctor {DoctorId} from database", appointmentDtos.Count, doctorId);
            return appointmentDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<List<AppointmentDto>> GetAppointmentsByPatientIdAsync(string patientId)
    {
        try
        {
            var cacheKey = $"appointments_patient_{patientId}";

            // Check cache first
            var cachedAppointments = await _cacheService.GetAsync<List<AppointmentDto>>(cacheKey);
            if (cachedAppointments != null)
            {
                _logger.LogDebug("Retrieved {Count} appointments for patient {PatientId} from cache", cachedAppointments.Count, patientId);
                return cachedAppointments;
            }

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId)
                .AsNoTracking()
                .ToListAsync();

            var appointmentDtos = _mapper.Map<List<AppointmentDto>>(appointments);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, appointmentDtos, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogInformation("Retrieved {Count} appointments for patient {PatientId} from database", appointmentDtos.Count, patientId);
            return appointmentDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for patient {PatientId}", patientId);
            throw;
        }
    }
    public async Task<List<AppointmentDto>> GetUpcomingAppointmentsAsync()
    {
        try
        {
            // Check cache first
            var cachedAppointments = await _cacheService.GetAsync<List<AppointmentDto>>(UPCOMING_APPOINTMENTS_CACHE_KEY);
            if (cachedAppointments != null)
            {
                _logger.LogDebug("Retrieved {Count} upcoming appointments from cache", cachedAppointments.Count);
                return cachedAppointments;
            }

            var now = DateTime.UtcNow;
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.StartTime > now && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.StartTime)
                .AsNoTracking()
                .ToListAsync();

            var appointmentDtos = _mapper.Map<List<AppointmentDto>>(appointments);

            // Cache the result with shorter expiry for time-sensitive data
            await _cacheService.SetAsync(UPCOMING_APPOINTMENTS_CACHE_KEY, appointmentDtos, TimeSpan.FromMinutes(5));

            _logger.LogInformation("Retrieved {Count} upcoming appointments from database", appointmentDtos.Count);
            return appointmentDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming appointments");
            throw;
        }
    }

    public async Task<List<AppointmentDto>> GetTodaysAppointmentsAsync()
    {
        try
        {
            // Check cache first
            var cachedAppointments = await _cacheService.GetAsync<List<AppointmentDto>>(TODAY_APPOINTMENTS_CACHE_KEY);
            if (cachedAppointments != null)
            {
                _logger.LogDebug("Retrieved {Count} today's appointments from cache", cachedAppointments.Count);
                return cachedAppointments;
            }

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.StartTime >= today && a.StartTime < tomorrow)
                .OrderBy(a => a.StartTime)
                .AsNoTracking()
                .ToListAsync();

            var appointmentDtos = _mapper.Map<List<AppointmentDto>>(appointments);

            // Cache the result with shorter expiry for time-sensitive data
            await _cacheService.SetAsync(TODAY_APPOINTMENTS_CACHE_KEY, appointmentDtos, TimeSpan.FromMinutes(5));

            _logger.LogInformation("Retrieved {Count} today's appointments from database", appointmentDtos.Count);
            return appointmentDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's appointments");
            throw;
        }
    }
    public async Task<List<AppointmentDto>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogDebug("Retrieving appointments between {StartDate} and {EndDate}", startDate, endDate);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.StartTime >= startDate && a.StartTime <= endDate)
                .OrderBy(a => a.StartTime)
                .AsNoTracking()
                .ToListAsync();

            var appointmentDtos = _mapper.Map<List<AppointmentDto>>(appointments);

            _logger.LogInformation("Found {Count} appointments between {StartDate} and {EndDate}", appointmentDtos.Count, startDate, endDate);
            return appointmentDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments between {StartDate} and {EndDate}", startDate, endDate);
            throw;
        }
    }
    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto appointmentDto)
    {
        try
        {
            _logger.LogDebug("Creating appointment for patient {PatientId} with doctor {DoctorId}", appointmentDto.PatientId, appointmentDto.DoctorId);

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

            // Use AutoMapper to create appointment
            var appointment = _mapper.Map<Appointment>(appointmentDto);

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Invalidate relevant caches
            await InvalidateAppointmentCaches();

            // Reload with related entities
            appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointment.Id);

            var appointmentDto_result = _mapper.Map<AppointmentDto>(appointment!);
            _logger.LogInformation("Created appointment {AppointmentId} for patient {PatientId} with doctor {DoctorId}", appointment!.Id, appointmentDto.PatientId, appointmentDto.DoctorId);

            return appointmentDto_result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment for patient {PatientId} with doctor {DoctorId}", appointmentDto.PatientId, appointmentDto.DoctorId);
            throw;
        }
    }
    public async Task<AppointmentDto> UpdateAppointmentAsync(string id, UpdateAppointmentDto appointmentDto)
    {
        try
        {
            _logger.LogDebug("Updating appointment {AppointmentId}", id);

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                throw new KeyNotFoundException($"Appointment with ID {id} not found");
            }

            // Use AutoMapper for partial updates
            _mapper.Map(appointmentDto, appointment);

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            // Invalidate relevant caches
            await InvalidateAppointmentCaches(appointment.Id, appointment.DoctorId, appointment.PatientId);

            var result = _mapper.Map<AppointmentDto>(appointment);
            _logger.LogInformation("Updated appointment {AppointmentId}", id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAppointmentAsync(string id)
    {
        try
        {
            _logger.LogDebug("Deleting appointment {AppointmentId}", id);

            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                _logger.LogWarning("Attempted to delete non-existent appointment {AppointmentId}", id);
                return false;
            }

            var doctorId = appointment.DoctorId;
            var patientId = appointment.PatientId;

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            // Invalidate relevant caches
            await InvalidateAppointmentCaches(id, doctorId, patientId);

            _logger.LogInformation("Deleted appointment {AppointmentId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appointment {AppointmentId}", id);
            throw;
        }
    }
    public async Task<AppointmentDto> UpdateAppointmentStatusAsync(string id, AppointmentStatus status)
    {
        try
        {
            _logger.LogInformation("Updating appointment status for ID: {Id} to {Status}", id, status);

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                _logger.LogWarning("Appointment not found for ID: {Id}", id);
                throw new KeyNotFoundException($"Appointment with ID {id} not found");
            }

            var oldStatus = appointment.Status;
            appointment.Status = status;
            appointment.UpdatedAt = DateTime.UtcNow;

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            // Invalidate related caches
            await InvalidateAppointmentCaches(id, appointment.DoctorId, appointment.PatientId);

            var appointmentDto = _mapper.Map<AppointmentDto>(appointment);

            _logger.LogInformation("Successfully updated appointment {Id} status from {OldStatus} to {NewStatus}",
                id, oldStatus, status);

            return appointmentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment status for ID: {Id}", id);
            throw;
        }
    }

    private async Task InvalidateAppointmentCaches(string? appointmentId = null, string? doctorId = null, string? patientId = null)
    {
        // Clear general caches
        await _cacheService.RemoveAsync(ALL_APPOINTMENTS_CACHE_KEY);
        await _cacheService.RemoveAsync(UPCOMING_APPOINTMENTS_CACHE_KEY);
        await _cacheService.RemoveAsync(TODAY_APPOINTMENTS_CACHE_KEY);

        // Clear specific caches if provided
        if (!string.IsNullOrEmpty(appointmentId))
        {
            await _cacheService.RemoveAsync($"appointment_{appointmentId}");
        }

        if (!string.IsNullOrEmpty(doctorId))
        {
            await _cacheService.RemoveAsync($"appointments_doctor_{doctorId}");
        }

        if (!string.IsNullOrEmpty(patientId))
        {
            await _cacheService.RemoveAsync($"appointments_patient_{patientId}");
        }
    }
}
