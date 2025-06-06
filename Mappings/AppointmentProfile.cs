using AutoMapper;
using HealthcareApi.DTOs;
using HealthcareApi.Models;

namespace HealthcareApi.Mappings;

/// <summary>
/// AutoMapper profile for Appointment entity mappings
/// </summary>
public class AppointmentProfile : Profile
{
    public AppointmentProfile()
    {
        // Appointment to AppointmentDto mapping
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // CreateAppointmentDto to Appointment mapping
        CreateMap<CreateAppointmentDto, Appointment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Patient, opt => opt.Ignore())
            .ForMember(dest => dest.Doctor, opt => opt.Ignore())
            .ForMember(dest => dest.Reminders, opt => opt.Ignore());        // UpdateAppointmentDto to Appointment mapping (for partial updates)
        CreateMap<UpdateAppointmentDto, Appointment>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
