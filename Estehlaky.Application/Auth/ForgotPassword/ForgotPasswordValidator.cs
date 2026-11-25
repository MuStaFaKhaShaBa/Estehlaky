using FluentValidation;

namespace Estehlaky.Application.Auth.ForgotPassword;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Email)
            .ValidEmail()
            .When(x => string.IsNullOrWhiteSpace(x.Phone));
    }
}