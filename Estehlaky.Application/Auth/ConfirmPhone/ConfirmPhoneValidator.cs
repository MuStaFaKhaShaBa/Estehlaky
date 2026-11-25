using FluentValidation;

namespace Estehlaky.Application.Auth.ConfirmPhone;

public sealed class ConfirmPhoneValidator : AbstractValidator<ConfirmPhoneCommand>
{
    public ConfirmPhoneValidator()
    {
        RuleFor(x => x.Otp).NotEmpty().WithMessage("OTP is required.");
    }
}
