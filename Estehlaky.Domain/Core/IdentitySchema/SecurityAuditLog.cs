using Estehlaky.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Estehlaky.Domain.Core.IdentitySchema
{
    public class SecurityAuditLog : BaseEntity
    {

        public Guid? UserId { get; set; }
        public AppUser? User { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public string? Details { get; set; }

        public bool IsSuspicious { get; set; }
    }

}