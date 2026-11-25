using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email, string? Phone) : IRequest<Result>;
