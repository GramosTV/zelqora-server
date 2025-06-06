using HealthcareApi.DTOs;
using HealthcareApi.Models;
using HealthcareApi.Services;
using HealthcareApi.Validators;
using HealthcareApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using FluentValidation;

namespace HealthcareApi.Controllers;

/// <summary>
/// Controller for managing appointment-related operations in the healthcare system.
/// Provides endpoints for appointment scheduling, management, and status updates.
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("Api")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;
    private readonly IValidator<CreateAppointmentDto> _createValidator;
    private readonly IValidator<UpdateAppointmentDto> _updateValidator;

    /// <summary>
    /// Initializes a new instance of the AppointmentsController.
    /// </summary>
    /// <param name="appointmentService">Service for appointment operations</param>
    /// <param name="logger">Logger for tracking operations and errors</param>
    /// <param name="createValidator">Validator for appointment creation</param>
    /// <param name="updateValidator">Validator for appointment updates</param>
    public AppointmentsController(
        IAppointmentService appointmentService,
        ILogger<AppointmentsController> logger,
        IValidator<CreateAppointmentDto> createValidator,
        IValidator<UpdateAppointmentDto> updateValidator)
    {
        _appointmentService = appointmentService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }    /// <summary>
         /// Retrieves appointments based on various filter criteria.
         /// Can filter by upcoming appointments, today's appointments, or date range.
         /// </summary>
         /// <param name="upcoming">If true, returns only upcoming appointments</param>
         /// <param name="today">If true, returns only today's appointments</param>
         /// <param name="startDate">Start date for date range filter (optional)</param>
         /// <param name="endDate">End date for date range filter (optional)</param>
         /// <returns>List of appointments matching the specified criteria</returns>
         /// <response code="200">Returns the list of appointments</response>
         /// <response code="401">If the user is not authenticated</response>
         /// <response code="500">If an unexpected error occurs</response>
    [HttpGet]
    [ProducesResponseType<IEnumerable<AppointmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]    /// <param name="endDate">End date for date range filter (optional)</param>
                                                                        /// <returns>List of appointments matching the specified criteria</returns>
                                                                        /// <response code="200">Returns the list of appointments</response>
                                                                        /// <response code="401">If the user is not authenticated</response>
                                                                        /// <response code="500">If an unexpected error occurs</response>
    [HttpGet]
    [ProducesResponseType<IEnumerable<AppointmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Retrieves a specific appointment by its ID.
    /// Users can only access appointments where they are either the doctor or patient (except admins).
    /// </summary>
    /// <param name="id">The unique identifier of the appointment</param>
    /// <returns>The appointment details if found and accessible</returns>
    /// <response code="200">Returns the appointment details</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to view this appointment</response>
    /// <response code="404">If the appointment is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("{id}")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAppointment(string id)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);

            if (appointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} not found" });
            }            // Check if the user has permission to view this appointment
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!RoleHelper.CanAccessAppointment(User, appointment.DoctorId, appointment.PatientId))
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

    /// <summary>
    /// Retrieves all appointments for a specific doctor.
    /// Only the doctor themselves or administrators can access this endpoint.
    /// </summary>
    /// <param name="doctorId">The unique identifier of the doctor</param>
    /// <returns>List of appointments for the specified doctor</returns>
    /// <response code="200">Returns the list of appointments for the doctor</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to view these appointments</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("doctor/{doctorId}")]
    [ProducesResponseType<IEnumerable<AppointmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAppointmentsByDoctor(string doctorId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Only the doctor themselves or an admin can see their appointments
            if (!RoleHelper.CanAccessUserResource(User, doctorId))
            {
                return Forbid();
            }

            var appointments = await _appointmentService.GetAppointmentsByDoctorIdAsync(doctorId);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor appointments");
            return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
        }
    }

    /// <summary>
    /// Retrieves all appointments for a specific patient.
    /// Only the patient themselves, doctors, or administrators can access this endpoint.
    /// </summary>
    /// <param name="patientId">The unique identifier of the patient</param>
    /// <returns>List of appointments for the specified patient</returns>
    /// <response code="200">Returns the list of appointments for the patient</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to view these appointments</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType<IEnumerable<AppointmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAppointmentsByPatient(string patientId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Only the patient themselves, their doctor, or an admin can see their appointments
            if (!RoleHelper.IsAdmin(User) && patientId != currentUserId)
            {
                // For simplicity, we'll just check if they're a doctor
                if (!RoleHelper.IsDoctor(User))
                {
                    return Forbid();
                }
            }

            var appointments = await _appointmentService.GetAppointmentsByPatientIdAsync(patientId);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient appointments");
            return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
        }
    }

    /// <summary>
    /// Retrieves all appointments for a specific user (doctor or patient).
    /// Users can only access their own appointments unless they are administrators.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>List of appointments for the specified user</returns>
    /// <response code="200">Returns the list of appointments for the user</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to view these appointments</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType<IEnumerable<AppointmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAppointmentsByUser(string userId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Users can only access their own appointments unless they're admins
            if (!RoleHelper.CanAccessUserResource(User, userId))
            {
                return Forbid();
            }

            // Determine if the user is a doctor or patient and call the appropriate method
            if (RoleHelper.IsDoctor(User))
            {
                var appointments = await _appointmentService.GetAppointmentsByDoctorIdAsync(userId);
                return Ok(appointments);
            }
            else if (RoleHelper.IsPatient(User))
            {
                var appointments = await _appointmentService.GetAppointmentsByPatientIdAsync(userId);
                return Ok(appointments);
            }
            else
            {
                // For admins or other roles, get all appointments
                var appointments = await _appointmentService.GetAllAppointmentsAsync();
                return Ok(appointments);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user appointments");
            return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
        }
    }

    /// <summary>
    /// Creates a new appointment in the system.
    /// Patients can only create appointments for themselves, doctors can create appointments they will attend.
    /// </summary>
    /// <param name="appointmentDto">The appointment creation details</param>
    /// <returns>The created appointment details</returns>
    /// <response code="201">Returns the newly created appointment</response>
    /// <response code="400">If the appointment data is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to create this appointment</response>
    /// <response code="409">If there's a scheduling conflict</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpPost]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAppointment(CreateAppointmentDto appointmentDto)
    {
        try
        {
            // Validate the appointment data
            var validationResult = await _createValidator.ValidateAsync(appointmentDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })
                });
            }

            // Check if the user has permission to create an appointment
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (RoleHelper.IsPatient(User) && appointmentDto.PatientId != currentUserId)
            {
                return Forbid();
            }

            if (RoleHelper.IsDoctor(User) && appointmentDto.DoctorId != currentUserId)
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

    /// <summary>
    /// Updates an existing appointment with new information.
    /// Only the doctor or patient involved in the appointment (or admins) can update it.
    /// </summary>
    /// <param name="id">The unique identifier of the appointment to update</param>
    /// <param name="appointmentDto">The updated appointment details</param>
    /// <returns>The updated appointment details</returns>
    /// <response code="200">Returns the updated appointment</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to update this appointment</response>
    /// <response code="404">If the appointment is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpPatch("{id}")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAppointment(string id, UpdateAppointmentDto appointmentDto)
    {
        try
        {
            // Validate the update data
            var validationResult = await _updateValidator.ValidateAsync(appointmentDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })
                });
            }

            // Check if the appointment exists
            var existingAppointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (existingAppointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} not found" });
            }

            // Check if the user has permission to update this appointment
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!RoleHelper.CanAccessAppointment(User, existingAppointment.DoctorId, existingAppointment.PatientId))
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

    /// <summary>
    /// Updates the status of an existing appointment.
    /// Only the doctor or patient involved in the appointment (or admins) can update the status.
    /// </summary>
    /// <param name="id">The unique identifier of the appointment</param>
    /// <param name="status">The new status for the appointment</param>
    /// <returns>The updated appointment details</returns>
    /// <response code="200">Returns the updated appointment</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to update this appointment</response>
    /// <response code="404">If the appointment is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpPatch("{id}/status")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAppointmentStatus(string id, [FromBody] AppointmentStatus status)
    {
        try
        {
            // Check if the appointment exists
            var existingAppointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (existingAppointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} not found" });
            }            // Check if the user has permission to update this appointment
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!RoleHelper.CanAccessAppointment(User, existingAppointment.DoctorId, existingAppointment.PatientId))
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

    /// <summary>
    /// Deletes an appointment from the system.
    /// Only the doctor or patient involved in the appointment (or admins) can delete it.
    /// </summary>
    /// <param name="id">The unique identifier of the appointment to delete</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the appointment was successfully deleted</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have permission to delete this appointment</response>
    /// <response code="404">If the appointment is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

            if (!RoleHelper.CanAccessAppointment(User, existingAppointment.DoctorId, existingAppointment.PatientId))
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
