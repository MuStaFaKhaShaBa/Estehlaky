using Estehlaky.Application.Common.Results;

namespace Estehlaky.UnitTests.Common;

public class ErrorTests
{
    [Fact]
    public void NotFound_has_a_default_code_and_type()
    {
        var error = Error.NotFound();

        Assert.Equal("General.NotFound", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);
    }

    [Fact]
    public void Unauthorized_and_Forbidden_use_their_own_types()
    {
        Assert.Equal(ErrorType.Unauthorized, Error.Unauthorized().Type);
        Assert.Equal("General.Unauthorized", Error.Unauthorized().Code);
        Assert.Equal(ErrorType.Forbidden, Error.Forbidden().Type);
        Assert.Equal("General.Forbidden", Error.Forbidden().Code);
    }

    [Fact]
    public void Conflict_keeps_the_code_and_description()
    {
        var error = Error.Conflict("X.Taken", "already taken");

        Assert.Equal("X.Taken", error.Code);
        Assert.Equal("already taken", error.Description);
        Assert.Equal(ErrorType.Conflict, error.Type);
    }

    [Fact]
    public void Validation_carries_the_field_errors()
    {
        var fields = new[] { new FieldError("email", "EmailValidator", "Bad email") };

        var error = Error.Validation(fields);

        Assert.Equal("General.Validation", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(fields, error.Fields);
    }

    [Fact]
    public void Confirmation_carries_the_reasons()
    {
        var error = Error.Confirmation(["large drop", "future timestamp"]);

        Assert.Equal("General.RequiresConfirmation", error.Code);
        Assert.Equal(ErrorType.ConfirmationRequired, error.Type);
        Assert.Equal(new[] { "large drop", "future timestamp" }, error.Reasons);
    }

    [Fact]
    public void Custom_stores_the_free_text_message()
    {
        var error = Error.Custom("Something specific", "My.Code");

        Assert.Equal("Something specific", error.CustomError);
        Assert.Equal("My.Code", error.Code);
        Assert.Equal(ErrorType.Custom, error.Type);
    }

    [Fact]
    public void Custom_uses_a_default_code_when_none_is_given()
    {
        Assert.Equal("General.Custom", Error.Custom("x").Code);
    }

    [Fact]
    public void Failure_is_a_generic_server_side_failure()
    {
        var error = Error.Failure("Seed.Broken", "broken");

        Assert.Equal(ErrorType.Failure, error.Type);
    }
}
