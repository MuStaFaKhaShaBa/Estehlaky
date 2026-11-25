using FluentValidation;

namespace Estehlaky.Application.Auth.ConfirmEmail;

public sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailValidator()
    {
        RuleFor(x => x.Otp).NotEmpty().WithMessage("OTP is required.");
    }
}
