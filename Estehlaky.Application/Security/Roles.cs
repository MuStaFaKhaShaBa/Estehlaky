
namespace Estehlaky.Application.Security
{
    /// <summary>
    /// Defines the application roles used for authentication,
    /// authorization, and access control.
    /// </summary>
    public static class Roles
    {
        /// <summary>Full system administration access.</summary>
        public const string Admin = "Admin";

        /// <summary>Standard application user.</summary>
        public const string User = "User";

        /// <summary>Creates, enters, and submits application records.</summary>
        public const string DataEntry = "DataEntry";

        /// <summary>Reviews submitted records and approves or rejects them.</summary>
        public const string Reviewer = "Reviewer";

        /// <summary>Supervises data entry and review operations.</summary>
        public const string Supervisor = "Supervisor";

        /// <summary>Manages operational activities and reporting.</summary>
        public const string Manager = "Manager";
    }
}
