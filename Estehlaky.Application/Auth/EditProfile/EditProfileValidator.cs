
using FluentValidation;

namespace Estehlaky.Application.Auth.EditProfile;

public sealed class EditProfileValidator : AbstractValidator<EditProfileCommand>
{
    private static readonly string[] Languages = ["ar", "en"];

    public EditProfileValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200)
            .When(x => x.FullName is not null)
            .WithMessage("Full name cannot exceed 200 characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{7,14}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Phone number must be a valid international phone number.");

        
        RuleFor(x => x.Language)
            .NotEmpty()
            .Must(language => Languages.Contains(language))
            .WithMessage("Language must be 'ar' or 'en'.");

        RuleFor(x => x.Client)
            .NotNull();
    }
}
