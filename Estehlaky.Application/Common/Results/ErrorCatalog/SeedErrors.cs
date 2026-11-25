namespace Estehlaky.Application.Common.Results.ErrorCatalog
{
    public static class SeedErrors
    {
        /// <summary>A role could not be created during seeding.</summary>
        public static readonly Error RoleCreationFailed =
            Error.Failure(
                "Seed.RoleCreationFailed",
                "Unable to create the required application role.");

        /// <summary>A user could not be created during seeding.</summary>
        public static readonly Error UserCreationFailed =
            Error.Failure(
                "Seed.UserCreationFailed",
                "Unable to create the required application user.");

        /// <summary>A role could not be assigned to a user.</summary>
        public static readonly Error RoleAssignmentFailed =
            Error.Failure(
                "Seed.RoleAssignmentFailed",
                "Unable to assign the required role to the user.");

        /// <summary>A required user or role could not be found.</summary>
        public static readonly Error EntityNotFound =
            Error.NotFound(
                "Seed.EntityNotFound",
                "A required entity was not found during seeding.");

        /// <summary>The seed configuration is invalid.</summary>
        public static readonly Error InvalidConfiguration =
            Error.Failure(
                "Seed.InvalidConfiguration",
                "The application seed configuration is invalid.");
    }
}