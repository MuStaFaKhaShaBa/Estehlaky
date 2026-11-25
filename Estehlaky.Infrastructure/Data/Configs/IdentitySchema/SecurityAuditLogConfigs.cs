
using Estehlaky.Domain.Core.IdentitySchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estehlaky.Infrastructure.Data.Configs.IdentitySchema
{
    internal class SecurityAuditLogConfigs
        : BaseEntityConfiguration<SecurityAuditLog>
    {
        public override void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
        {
            base.Configure(builder);

            builder.ToTable("SecurityAuditLogs", schema: "Identity");

            builder.Property(x => x.EventType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.IpAddress)
                .HasMaxLength(45);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(500);

        }
    }
}
