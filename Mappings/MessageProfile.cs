using AutoMapper;
using HealthcareApi.DTOs;
using HealthcareApi.Models;

namespace HealthcareApi.Mappings;

/// <summary>
/// AutoMapper profile for Message entity mappings
/// </summary>
public class MessageProfile : Profile
{
    public MessageProfile()
    {
        // Message to MessageDto mapping
        CreateMap<Message, MessageDto>();

        // CreateMessageDto to Message mapping
        CreateMap<CreateMessageDto, Message>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SenderId, opt => opt.Ignore()) // Set from controller
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Read, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.Sender, opt => opt.Ignore())
            .ForMember(dest => dest.Receiver, opt => opt.Ignore());        // UpdateMessageDto to Message mapping
        CreateMap<UpdateMessageDto, Message>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
