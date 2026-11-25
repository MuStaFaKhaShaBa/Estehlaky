using FluentValidation;

namespace Estehlaky.Application.Auth.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    private static readonly string[] Languages = ["ar", "en"];

    public RegisterValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).ValidPassword();
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.Language).Must(l => Languages.Contains(l)).WithMessage("Language must be 'ar' or 'en'.");
    }
}
