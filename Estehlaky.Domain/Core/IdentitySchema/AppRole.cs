using Microsoft.AspNetCore.Identity;

namespace Estehlaky.Domain.Core.IdentitySchema;

/// <summary>Kept for Identity. Property roles (Owner/Member) live in PropertyMembers, not here.</summary>
public class AppRole : IdentityRole<Guid>;
