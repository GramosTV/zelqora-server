using HealthcareApi.Models;
using System.Security.Claims;

namespace HealthcareApi.Helpers;

/// <summary>
/// Utility class for role-based operations using enums instead of magic strings.
/// Provides type-safe role checking and authorization helpers.
/// </summary>
public static class RoleHelper
{
    /// <summary>
    /// Gets the user's role from the ClaimsPrincipal as an enum.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>The user's role as enum, or null if not found</returns>
    public static UserRole? GetUserRole(ClaimsPrincipal user)
    {
        var roleString = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(roleString))
            return null;

        return Enum.TryParse<UserRole>(roleString, true, out var role) ? role : null;
    }

    /// <summary>
    /// Checks if the user has the specified role.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <param name="role">The role to check for</param>
    /// <returns>True if the user has the role</returns>
    public static bool IsInRole(ClaimsPrincipal user, UserRole role)
    {
        var userRole = GetUserRole(user);
        return userRole == role;
    }

    /// <summary>
    /// Checks if the user is an administrator.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>True if the user is an admin</returns>
    public static bool IsAdmin(ClaimsPrincipal user)
    {
        return IsInRole(user, UserRole.Admin);
    }

    /// <summary>
    /// Checks if the user is a doctor.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>True if the user is a doctor</returns>
    public static bool IsDoctor(ClaimsPrincipal user)
    {
        return IsInRole(user, UserRole.Doctor);
    }

    /// <summary>
    /// Checks if the user is a patient.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>True if the user is a patient</returns>
    public static bool IsPatient(ClaimsPrincipal user)
    {
        return IsInRole(user, UserRole.Patient);
    }

    /// <summary>
    /// Gets the role name as string for authorization attributes.
    /// </summary>
    /// <param name="role">The role enum</param>
    /// <returns>The role name as string</returns>
    public static string GetRoleName(UserRole role)
    {
        return role.ToString();
    }

    /// <summary>
    /// Gets all role names as comma-separated string for authorization attributes.
    /// </summary>
    /// <param name="roles">The roles to include</param>
    /// <returns>Comma-separated role names</returns>
    public static string GetRoleNames(params UserRole[] roles)
    {
        return string.Join(",", roles.Select(r => r.ToString()));
    }

    /// <summary>
    /// Checks if the user can access resource belonging to the specified user ID.
    /// Admins can access any resource, others can only access their own.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <param name="resourceUserId">The user ID of the resource owner</param>
    /// <returns>True if access is allowed</returns>
    public static bool CanAccessUserResource(ClaimsPrincipal user, string resourceUserId)
    {
        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return IsAdmin(user) || currentUserId == resourceUserId;
    }

    /// <summary>
    /// Checks if the user can access appointment resource.
    /// Admins can access any appointment, doctors/patients can access their own appointments.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <param name="doctorId">The doctor ID of the appointment</param>
    /// <param name="patientId">The patient ID of the appointment</param>
    /// <returns>True if access is allowed</returns>
    public static bool CanAccessAppointment(ClaimsPrincipal user, string doctorId, string patientId)
    {
        if (IsAdmin(user))
            return true;

        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return currentUserId == doctorId || currentUserId == patientId;
    }
}
