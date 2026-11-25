namespace Estehlaky.Application.Sync;

public enum SyncItemStatus { Applied, AlreadyApplied, Rejected, Conflict }

public sealed record SyncItemResultDto(Guid Id, SyncItemStatus Status, string? ErrorCode);
