using Estehlaky.Domain.Core.IdentitySchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estehlaky.Infrastructure.Data.Configs.IdentitySchema;

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens", "Identity");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();   // ids are created in code (Guid v7)

        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.RevokedReason).HasMaxLength(50);
        b.Property(x => x.CreatedByIp).HasMaxLength(45);
        b.Property(x => x.UserAgent).HasMaxLength(256);

        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.UserId, x.FamilyId });
        b.HasIndex(x => x.ExpiresAt);                  // for a cleanup job later

        b.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
