using FluentValidation;

namespace Estehlaky.Application.Auth.Refresh;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
}
