using Estehlaky.Application.Common.Results;

namespace Estehlaky.UnitTests.Common;

public class ResultTests
{
    private static readonly Error First = Error.NotFound("Test.First", "first");
    private static readonly Error Second = Error.Conflict("Test.Second", "second");

    [Fact]
    public void Success_has_no_errors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Success_has_no_primary_error()
    {
        var result = Result.Success();

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_exposes_the_first_error_as_the_primary_error()
    {
        var result = Result.Failure(new[] { First, Second });

        Assert.True(result.IsFailure);
        Assert.Equal(First, result.Error);
        Assert.Equal(new[] { First, Second }, result.Errors);
    }

    [Fact]
    public void Failure_without_any_error_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Array.Empty<Error>()));
    }

    [Fact]
    public void Generic_success_carries_its_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Generic_failure_has_no_value()
    {
        var result = Result.Failure<int>(First);

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void A_value_converts_implicitly_to_a_successful_result()
    {
        Result<int> result = 7;

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void An_error_converts_implicitly_to_a_failed_result()
    {
        Result<int> generic = First;
        Result plain = Second;

        Assert.Equal(First, generic.Error);
        Assert.Equal(Second, plain.Error);
    }

    [Fact]
    public void Map_transforms_the_value_of_a_success()
    {
        var mapped = Result.Success(2).Map(x => x * 10);

        Assert.Equal(20, mapped.Value);
    }

    [Fact]
    public void Map_keeps_the_errors_of_a_failure_and_never_runs_the_mapper()
    {
        var called = false;

        var mapped = Result.Failure<int>(First).Map(x =>
        {
            called = true;
            return x;
        });

        Assert.False(called);
        Assert.Equal(First, mapped.Error);
    }

    [Fact]
    public void Match_picks_the_branch_that_fits_the_result()
    {
        var ok = Result.Success(5).Match(v => $"ok {v}", errors => "failed");
        var bad = Result.Failure<int>(First).Match(v => "ok", errors => $"failed {errors.Count}");

        Assert.Equal("ok 5", ok);
        Assert.Equal("failed 1", bad);
    }

    [Fact]
    public void Combine_of_successes_is_a_success()
    {
        var combined = Result.Combine(Result.Success(), Result.Success());

        Assert.True(combined.IsSuccess);
    }

    [Fact]
    public void Combine_merges_the_errors_of_every_failed_result()
    {
        var combined = Result.Combine(Result.Failure(First), Result.Success(), Result.Failure(Second));

        Assert.True(combined.IsFailure);
        Assert.Equal(new[] { First, Second }, combined.Errors);
    }
}
