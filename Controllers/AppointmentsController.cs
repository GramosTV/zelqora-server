using HealthcareApi.DTOs;
using HealthcareApi.Models;
using HealthcareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthcareApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IAppointmentService appointmentService, ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments([FromQuery] bool? upcoming, [FromQuery] bool? today,
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            if (upcoming == true)
            {
                var upcomingAppointments = await _appointmentService.GetUpcomingAppointmentsAsync();
                return Ok(upcomingAppointments);
            }

            if (today == true)
            {
                var todaysAppointments = await _appointmentService.GetTodaysAppointmentsAsync();
                return Ok(todaysAppointments);
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                var appointmentsInRange = await _appointmentService.GetAppointmentsByDateRangeAsync(startDate.Value, endDate.Value);
                return Ok(appointmentsInRange);
            }

            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments");
            return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointment(string id)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);

            if (appointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} not found" });
            }

            // Check if the user has permission to view this appointment
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" &&
                appointment.DoctorId != currentUserId &&
                appointment.PatientId != currentUserId)
            {
                return Forbid();
            }

            return Ok(appointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointment {AppointmentId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the appointment" });
        }
    }

    [HttpGet("doctor/{doctorId}")]
    public async Task<IActionResult> GetAppointmentsByDoctor(string doctorId)
    {
        try
        {
            // Check if the user has permission to view these appointments
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && doctorId != currentUserId)
            {
                return Forbid();
            }

            var appointments = await _appointmentService.GetAppointmentsByDoctorIdAsync(doctorId);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for doctor {DoctorId}", doctorId);
            return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
        }
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetAppointmentsByPatient(string patientId)
    {
        try
        {
            // Check if the user has permission to view these appointments
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && userRole != "Doctor" && patientId != currentUserId)
            {
                return Forbid();
            }

            var appointments = await _appointmentService.GetAppointmentsByPatientIdAsync(patientId);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for patient {PatientId}", patientId);
            return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetAppointmentsByUser(string userId)
    {
        try
        {
            // Check if the user has permission to view these appointments
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && userId != currentUserId)
            {
                return Forbid();
            }

            // Check if the user is a doctor or patient
            if (userRole == "Doctor")
            {
                var appointments = await _appointmentService.GetAppointmentsByDoctorIdAsync(userId);
                return Ok(appointments);
            }
            else if (userRole == "Patient")
            {
                var appointments = await _appointmentService.GetAppointmentsByPatientIdAsync(userId);
                return Ok(appointments);
            }
            else
            {
                var appointments = await _appointmentService.GetAllAppointmentsAsync();
                return Ok(appointments);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment(CreateAppointmentDto appointmentDto)
    {
        try
        {
            // Check if the user has permission to create an appointment
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole == "Patient" && appointmentDto.PatientId != currentUserId)
            {
                return Forbid();
            }

            if (userRole == "Doctor" && appointmentDto.DoctorId != currentUserId)
            {
                return Forbid();
            }

            var appointment = await _appointmentService.CreateAppointmentAsync(appointmentDto);
            return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            return StatusCode(500, new { message = "An error occurred while creating the appointment" });
        }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateAppointment(string id, UpdateAppointmentDto appointmentDto)
    {
        try
        {
            // Check if the appointment exists
            var existingAppointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (existingAppointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} not found" });
            }

            // Check if the user has permission to update this appointment
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" &&
                existingAppointment.DoctorId != currentUserId &&
                existingAppointment.PatientId != currentUserId)
            {
                return Forbid();
            }

            var appointment = await _appointmentService.UpdateAppointmentAsync(id, appointmentDto);
            return Ok(appointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the appointment" });
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateAppointmentStatus(string id, [FromBody] AppointmentStatus status)
    {
        try
        {
            // Check if the appointment exists
            var existingAppointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (existingAppointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} not found" });
            }

            // Check if the user has permission to update this appointment
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" &&
                existingAppointment.DoctorId != currentUserId &&
                existingAppointment.PatientId != currentUserId)
            {
                return Forbid();
            }

            var appointment = await _appointmentService.UpdateAppointmentStatusAsync(id, status);
            return Ok(appointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment status {AppointmentId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the appointment status" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(string id)
    {
        try
        {
            // Check if the appointment exists
            var existingAppointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (existingAppointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} not found" });
            }

            // Check if the user has permission to delete this appointment
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" &&
                existingAppointment.DoctorId != currentUserId &&
                existingAppointment.PatientId != currentUserId)
            {
                return Forbid();
            }

            var result = await _appointmentService.DeleteAppointmentAsync(id);

            if (!result)
            {
                return NotFound(new { message = $"Appointment with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appointment {AppointmentId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the appointment" });
        }
    }
}
