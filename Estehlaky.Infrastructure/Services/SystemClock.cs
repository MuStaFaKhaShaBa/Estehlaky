using Estehlaky.Application.Abstractions.Services;

namespace Estehlaky.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
