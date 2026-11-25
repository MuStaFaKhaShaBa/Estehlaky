using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ConfirmEmail;

public sealed record RequestEmailConfirmationCommand() : IRequest<Result>;
public sealed record ConfirmEmailCommand(string Otp) : IRequest<Result>;
