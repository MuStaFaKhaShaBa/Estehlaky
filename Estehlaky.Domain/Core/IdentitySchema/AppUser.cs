using Estehlaky.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Estehlaky.Domain.Core.IdentitySchema
{
    public class AppUser : IdentityUser<Guid>
    {
        public string? FullName { get; set; }
        public string PreferredLanguage { get; set; } = "ar";
        public string? ProfilePictureUrl { get; set; }
        public ThemeMode Theme { get; set; } = ThemeMode.System;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        /// <summary>Soft delete marker. Deleted users cannot sign in or refresh.</summary>
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
