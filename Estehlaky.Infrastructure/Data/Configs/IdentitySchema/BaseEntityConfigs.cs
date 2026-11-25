using Estehlaky.Domain.Common;
using Estehlaky.Domain.Core.IdentitySchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estehlaky.Infrastructure.Data.Configs
{
    public abstract class BaseEntityConfiguration<TEntity>
        : IEntityTypeConfiguration<TEntity>
        where TEntity : BaseEntity
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetimeoffset")
                .HasDefaultValueSql(
                    "TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00')")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetimeoffset")
                .IsRequired(false);
        }
       
    }
}