using Estehlaky.Domain.Core.IdentitySchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estehlaky.Infrastructure.Data.Configs.IdentitySchema
{
    /// <summary>
    /// Configures the database mapping and constraints for password reset OTP records.
    /// </summary>
    public sealed class ConfirmationOtpsConfiguration
        : IEntityTypeConfiguration<ConfirmationOtps>
    {
        public void Configure(EntityTypeBuilder<ConfirmationOtps> builder)
        {
            builder.ToTable("PasswordResetOtps","Identity");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.CodeHash)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.Purpose)
                .HasConversion<byte>()
                .IsRequired(); 

            builder.Property(x => x.UsedAt);

            builder.Property(x => x.FailedAttempts)
                .HasDefaultValue(0)
                .IsRequired();

            // Efficiently retrieve OTP records belonging to a user.
            builder.HasIndex(x => new { x.Purpose, x.UserId, x.CreatedAt })
                .HasDatabaseName("IX_PasswordResetOtps_UserId_CreatedAt");

            // Efficiently identify expired OTP records during cleanup.
            builder.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("IX_PasswordResetOtps_ExpiresAt");

            // Supports queries for active, unused OTP records.
            builder.HasIndex(x => new { x.UserId, x.UsedAt })
                .HasDatabaseName("IX_PasswordResetOtps_UserId_UsedAt");

            // Ensure that each OTP belongs to an existing user.
            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
