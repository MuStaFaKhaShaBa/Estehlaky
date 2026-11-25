
using Estehlaky.Domain.Core.IdentitySchema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estehlaky.Infrastructure.Data.Configs.IdentitySchema;

internal sealed class AppUserConfigs : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users", schema: "Identity");

        builder.Property(x => x.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("ar")
            .IsRequired();

        builder.Property(x => x.Theme)
            .HasConversion<byte>();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetimeoffset")
            .HasDefaultValueSql(
                "TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00')")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetimeoffset")
            .IsRequired(false);


        builder.Property(x => x.DeletedAt)
            .HasColumnType("datetimeoffset")
            .IsRequired(false);

        builder.HasIndex(x => x.PhoneNumber).IsUnique(true)
            .HasDatabaseName("PhoneIndex");

        builder.HasIndex(x => x.NormalizedEmail)
            .HasDatabaseName("EmailIndex");
    }
}
