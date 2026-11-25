using Estehlaky.Application.Common.Results;
using FluentValidation.Results;

namespace Estehlaky.UnitTests.Common;

public class ValidationConvertersTests
{
    [Fact]
    public void Property_names_become_camelCase_field_names()
    {
        var failure = new ValidationFailure("FullName", "Too long") { ErrorCode = "MaximumLengthValidator" };

        var error = new[] { failure }.ToError();

        var field = Assert.Single(error.Fields!);
        Assert.Equal("fullName", field.Field);
        Assert.Equal("MaximumLengthValidator", field.Code);
        Assert.Equal("Too long", field.Message);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Fact]
    public void A_ValidationResult_converts_all_of_its_failures()
    {
        var validation = new ValidationResult(new[]
        {
            new ValidationFailure("Email", "bad"),
            new ValidationFailure("Password", "short")
        });

        var error = validation.ToError();

        Assert.Equal(2, error.Fields!.Count);
        Assert.Equal(new[] { "email", "password" }, error.Fields.Select(f => f.Field));
    }
}
