using AutoMapper;
using HealthcareApi.DTOs;
using HealthcareApi.Models;

namespace HealthcareApi.Mappings;

/// <summary>
/// AutoMapper profile for User entity mappings
/// </summary>
public class UserProfile : Profile
{
    public UserProfile()
    {
        // User to UserDto mapping
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        // UserRegistrationDto to User mapping
        CreateMap<UserRegistrationDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokenExpiry, opt => opt.Ignore())
            .ForMember(dest => dest.ProfilePicture, opt => opt.Ignore())
            .ForMember(dest => dest.PatientAppointments, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorAppointments, opt => opt.Ignore())
            .ForMember(dest => dest.SentMessages, opt => opt.Ignore())
            .ForMember(dest => dest.ReceivedMessages, opt => opt.Ignore())
            .ForMember(dest => dest.Reminders, opt => opt.Ignore());        // UserUpdateDto to User mapping (for partial updates)
        CreateMap<UserUpdateDto, User>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // User to DoctorDto mapping
        CreateMap<User, DoctorDto>();
    }
}
