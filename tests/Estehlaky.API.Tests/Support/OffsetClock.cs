using Estehlaky.Application.Abstractions;
using Estehlaky.Application.Abstractions.Services;

namespace Estehlaky.API.Tests.Support;

/// <summary>
/// The real clock plus an adjustable offset, so tests can "wait" 11 seconds or 31 days without sleeping.
/// Every test class resets it when it finishes.
/// </summary>
public sealed class OffsetClock : IClock
{
    private long _offsetTicks;

    public DateTime UtcNow => DateTime.UtcNow.AddTicks(Interlocked.Read(ref _offsetTicks));

    public void Advance(TimeSpan by) => Interlocked.Add(ref _offsetTicks, by.Ticks);

    public void Reset() => Interlocked.Exchange(ref _offsetTicks, 0);
}
