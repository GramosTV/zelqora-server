using FluentValidation;
using HealthcareApi.DTOs;

namespace HealthcareApi.Validators;

/// <summary>
/// Validator for create message DTO
/// </summary>
public class CreateMessageDtoValidator : AbstractValidator<CreateMessageDto>
{
    public CreateMessageDtoValidator()
    {
        RuleFor(x => x.ReceiverId)
            .NotEmpty().WithMessage("Receiver ID is required");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(2000).WithMessage("Message content must not exceed 2000 characters");

        RuleFor(x => x.IntegrityHash)
            .MaximumLength(500).WithMessage("Integrity hash must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.IntegrityHash));
    }
}

/// <summary>
/// Validator for update message DTO
/// </summary>
public class UpdateMessageDtoValidator : AbstractValidator<UpdateMessageDto>
{
    public UpdateMessageDtoValidator()
    {
        // UpdateMessageDto only has Read property
        // No validation needed for boolean property
    }
}
