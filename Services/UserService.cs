using AutoMapper;
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

/// <summary>
/// Enhanced user service with caching, AutoMapper, and comprehensive error handling
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UserService> _logger;

    private const string ALL_USERS_CACHE_KEY = "all_users";
    private const string DOCTORS_CACHE_KEY = "doctors";
    private const string PATIENTS_CACHE_KEY = "patients";
    private const int CACHE_EXPIRY_MINUTES = 30;

    public UserService(
        ApplicationDbContext context,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<UserService> logger)
    {
        _context = context;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
    }
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            // Check cache first
            var cachedUsers = await _cacheService.GetAsync<List<UserDto>>(ALL_USERS_CACHE_KEY);
            if (cachedUsers != null)
            {
                _logger.LogDebug("Retrieved {Count} users from cache", cachedUsers.Count);
                return cachedUsers;
            }

            var users = await _context.Users.AsNoTracking().ToListAsync();
            var userDtos = _mapper.Map<List<UserDto>>(users);

            // Cache the result
            await _cacheService.SetAsync(ALL_USERS_CACHE_KEY, userDtos, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogInformation("Retrieved {Count} users from database", userDtos.Count);
            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            throw;
        }
    }
    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        try
        {
            var cacheKey = $"user_{id}";

            // Check cache first
            var cachedUser = await _cacheService.GetAsync<UserDto>(cacheKey);
            if (cachedUser != null)
            {
                _logger.LogDebug("Retrieved user {UserId} from cache", id);
                return cachedUser;
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                return null;
            }

            var userDto = _mapper.Map<UserDto>(user);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, userDto, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogDebug("Retrieved user {UserId} from database", id);
            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            throw;
        }
    }
    public async Task<List<UserDto>> GetDoctorsAsync()
    {
        try
        {
            // Check cache first
            var cachedDoctors = await _cacheService.GetAsync<List<UserDto>>(DOCTORS_CACHE_KEY);
            if (cachedDoctors != null)
            {
                _logger.LogDebug("Retrieved {Count} doctors from cache", cachedDoctors.Count);
                return cachedDoctors;
            }

            var doctors = await _context.Users
                .Where(u => u.Role == UserRole.Doctor)
                .AsNoTracking()
                .ToListAsync();

            var doctorDtos = _mapper.Map<List<UserDto>>(doctors);

            // Cache the result
            await _cacheService.SetAsync(DOCTORS_CACHE_KEY, doctorDtos, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogInformation("Retrieved {Count} doctors from database", doctorDtos.Count);
            return doctorDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors");
            throw;
        }
    }

    public async Task<List<DoctorDto>> GetDoctorListAsync()
    {
        try
        {
            var cacheKey = "doctor_list";

            // Check cache first
            var cachedDoctorList = await _cacheService.GetAsync<List<DoctorDto>>(cacheKey);
            if (cachedDoctorList != null)
            {
                _logger.LogDebug("Retrieved {Count} doctors list from cache", cachedDoctorList.Count);
                return cachedDoctorList;
            }

            var doctors = await _context.Users
                .Where(u => u.Role == UserRole.Doctor)
                .AsNoTracking()
                .ToListAsync();

            var doctorDtos = _mapper.Map<List<DoctorDto>>(doctors);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, doctorDtos, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogInformation("Retrieved {Count} doctors list from database", doctorDtos.Count);
            return doctorDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors list");
            throw;
        }
    }

    public async Task<List<UserDto>> GetPatientsAsync()
    {
        try
        {
            // Check cache first
            var cachedPatients = await _cacheService.GetAsync<List<UserDto>>(PATIENTS_CACHE_KEY);
            if (cachedPatients != null)
            {
                _logger.LogDebug("Retrieved {Count} patients from cache", cachedPatients.Count);
                return cachedPatients;
            }

            var patients = await _context.Users
                .Where(u => u.Role == UserRole.Patient)
                .AsNoTracking()
                .ToListAsync();

            var patientDtos = _mapper.Map<List<UserDto>>(patients);

            // Cache the result
            await _cacheService.SetAsync(PATIENTS_CACHE_KEY, patientDtos, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            _logger.LogInformation("Retrieved {Count} patients from database", patientDtos.Count);
            return patientDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patients");
            throw;
        }
    }
    public async Task<UserDto> UpdateUserAsync(string id, UserUpdateDto userUpdateDto)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found");
            }

            // Use AutoMapper for partial updates
            _mapper.Map(userUpdateDto, user);

            // Handle email uniqueness check
            if (!string.IsNullOrEmpty(userUpdateDto.Email))
            {
                if (await _context.Users.AnyAsync(u => u.Email == userUpdateDto.Email && u.Id != id))
                {
                    throw new ApplicationException("Email is already in use");
                }
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Clear relevant caches
            await InvalidateUserCaches(user.Id);

            var userDto = _mapper.Map<UserDto>(user);
            _logger.LogInformation("Updated user {UserId}", id);

            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            throw;
        }
    }

    public async Task<UserDto> CreateUserAsync(UserRegistrationDto userDto)
    {
        try
        {
            // Check if email is already in use
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                throw new ApplicationException("Email is already in use");
            }

            var user = _mapper.Map<User>(userDto);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Clear relevant caches
            await InvalidateUserCaches();

            var createdUserDto = _mapper.Map<UserDto>(user);
            _logger.LogInformation("Created user {UserId} with email {Email}", user.Id, user.Email);

            return createdUserDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email {Email}", userDto.Email);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                _logger.LogWarning("Attempted to delete non-existent user {UserId}", id);
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Clear relevant caches
            await InvalidateUserCaches(id);

            _logger.LogInformation("Deleted user {UserId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            throw;
        }
    }

    private async Task InvalidateUserCaches(string? userId = null)
    {
        // Clear general caches
        await _cacheService.RemoveAsync(ALL_USERS_CACHE_KEY);
        await _cacheService.RemoveAsync(DOCTORS_CACHE_KEY);
        await _cacheService.RemoveAsync(PATIENTS_CACHE_KEY);
        await _cacheService.RemoveAsync("doctor_list");

        // Clear specific user cache if provided
        if (!string.IsNullOrEmpty(userId))
        {
            await _cacheService.RemoveAsync($"user_{userId}");
        }
    }
    public async Task<UserDto> UpdateProfilePictureAsync(string id, string pictureUrl)
    {
        try
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

            // Clear relevant caches
            await InvalidateUserCaches(user.Id);

            var userDto = _mapper.Map<UserDto>(user);
            _logger.LogInformation("Updated profile picture for user {UserId}", id);

            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile picture for user {UserId}", id);
            throw;
        }
    }

    public async Task<List<UserDto>> SearchUsersAsync(string query)
    {
        try
        {
            _logger.LogDebug("Searching users with query: {Query}", query);

            var users = await _context.Users
                .Where(u => u.Email.Contains(query) ||
                       u.FirstName.Contains(query) ||
                       u.LastName.Contains(query))
                .AsNoTracking()
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserDto>>(users);
            _logger.LogInformation("Found {Count} users matching query '{Query}'", userDtos.Count, query);

            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with query '{Query}'", query);
            throw;
        }
    }
}
