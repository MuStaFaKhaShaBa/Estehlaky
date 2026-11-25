using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;
