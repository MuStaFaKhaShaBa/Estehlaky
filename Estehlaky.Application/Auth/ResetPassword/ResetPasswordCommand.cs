using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string? Phone, string Token, string NewPassword) : IRequest<Result>;
