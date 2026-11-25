using Estehlaky.Domain.Enums;

namespace Estehlaky.Domain.Core.IdentitySchema
{
    public sealed class ConfirmationOtps
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string CodeHash { get; set; } = string.Empty;

        public PurposeOfOTP Purpose { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public int FailedAttempts { get; set; }
    }
}