using HealthcareApi.Data;
using HealthcareApi.DTOs;
using HealthcareApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthcareApi.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<List<UserDto>> GetDoctorsAsync();
    Task<List<DoctorDto>> GetDoctorListAsync(); // Add new method for DoctorDto
    Task<List<UserDto>> GetPatientsAsync();
    Task<UserDto> UpdateUserAsync(string id, UserUpdateDto userUpdateDto);
    Task<UserDto> CreateUserAsync(UserRegistrationDto userDto);
    Task<bool> DeleteUserAsync(string id);
    Task<UserDto> UpdateProfilePictureAsync(string id, string pictureUrl);
    Task<List<UserDto>> SearchUsersAsync(string query);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users.ToListAsync();
        return users.Select(MapUserToDto).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _context.Users.FindAsync(id);
        return user != null ? MapUserToDto(user) : null;
    }

    public async Task<List<UserDto>> GetDoctorsAsync()
    {
        var doctors = await _context.Users
            .Where(u => u.Role == UserRole.Doctor)
            .ToListAsync();

        return doctors.Select(MapUserToDto).ToList();
    }

    public async Task<List<DoctorDto>> GetDoctorListAsync() // Implement new method
    {
        var doctors = await _context.Users
            .Where(u => u.Role == UserRole.Doctor)
            .Select(u => new DoctorDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Specialization = u.Specialization
            })
            .ToListAsync();
        return doctors;
    }

    public async Task<List<UserDto>> GetPatientsAsync()
    {
        var patients = await _context.Users
            .Where(u => u.Role == UserRole.Patient)
            .ToListAsync();

        return patients.Select(MapUserToDto).ToList();
    }

    public async Task<UserDto> UpdateUserAsync(string id, UserUpdateDto userUpdateDto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(userUpdateDto.FirstName))
            user.FirstName = userUpdateDto.FirstName;

        if (!string.IsNullOrEmpty(userUpdateDto.LastName))
            user.LastName = userUpdateDto.LastName;

        if (!string.IsNullOrEmpty(userUpdateDto.Email))
        {
            // Check if email is already in use by another user
            if (await _context.Users.AnyAsync(u => u.Email == userUpdateDto.Email && u.Id != id))
            {
                throw new ApplicationException("Email is already in use");
            }

            user.Email = userUpdateDto.Email;
        }

        if (!string.IsNullOrEmpty(userUpdateDto.Specialization) && user.Role == UserRole.Doctor)
            user.Specialization = userUpdateDto.Specialization;

        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return MapUserToDto(user);
    }

    public async Task<UserDto> CreateUserAsync(UserRegistrationDto userDto)
    {
        // Check if email is already in use
        if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
        {
            throw new ApplicationException("Email is already in use");
        }

        var user = new User
        {
            Email = userDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Role = userDto.Role,
            Specialization = userDto.Specialization
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapUserToDto(user);
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<UserDto> UpdateProfilePictureAsync(string id, string pictureUrl)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found");
        }

        user.ProfilePicture = pictureUrl;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return MapUserToDto(user);
    }

    public async Task<List<UserDto>> SearchUsersAsync(string query)
    {
        var users = await _context.Users
            .Where(u => u.Email.Contains(query) ||
                   u.FirstName.Contains(query) ||
                   u.LastName.Contains(query))
            .ToListAsync();

        return users.Select(MapUserToDto).ToList();
    }

    private static UserDto MapUserToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            ProfilePicture = user.ProfilePicture,
            Specialization = user.Specialization,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
