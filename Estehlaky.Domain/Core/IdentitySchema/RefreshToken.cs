namespace Estehlaky.Domain.Core.IdentitySchema;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Client device. No foreign key until core.UserDevices exists.</summary>
    public Guid? DeviceId { get; set; }

    /// <summary>SHA-256 (hex) of the token. The raw token is never stored.</summary>
    public string TokenHash { get; set; } = default!;

    /// <summary>All tokens issued from one login share a family (one device session).</summary>
    public Guid FamilyId { get; set; }
    public DateTime FamilyStartedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }
}
