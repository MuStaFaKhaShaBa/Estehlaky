using Estehlaky.Application.Common.Results;

namespace Estehlaky.Application.Sync;

public static class SyncResultConverters
{
    public static SyncItemResultDto ToSyncItem(this Result result, Guid id) =>
        result.IsSuccess
            ? new SyncItemResultDto(id, SyncItemStatus.Applied, null)
            : new SyncItemResultDto(
                id,
                result.Error.Type == ErrorType.Conflict ? SyncItemStatus.Conflict : SyncItemStatus.Rejected,
                result.Error.Code);
}
