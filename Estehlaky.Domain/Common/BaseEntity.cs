using System;
using System.Collections.Generic;
using System.Text;

namespace Estehlaky.Domain.Common
{
    public  class BaseEntity
    {
        public Guid Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
