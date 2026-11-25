using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ConfirmPhone;

public sealed record RequestPhoneConfirmationCommand() : IRequest<Result>;
public sealed record ConfirmPhoneCommand(string Otp) : IRequest<Result>;
 