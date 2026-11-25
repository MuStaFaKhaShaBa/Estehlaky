using Estehlaky.Domain.Core.IdentitySchema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Estehlaky.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ConfirmationOtps> ConfirmationOtps =>
    Set<ConfirmationOtps>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.HasDefaultSchema("core");

        b.Entity<AppRole>().ToTable("Roles", "Identity");
        b.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "Identity");
        b.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "Identity");
        b.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "Identity");
        b.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "Identity");
        b.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "Identity");

        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
