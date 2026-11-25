namespace Estehlaky.Application.Abstractions.AppSettingsModels;

/// <summary>Where a request came from. Built by the API layer so handlers never touch HttpContext.</summary>
public sealed record ClientInfo(Guid? DeviceId, string? IpAddress, string? UserAgent);
