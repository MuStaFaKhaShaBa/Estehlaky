using Estehlaky.Application.Common.Results;

namespace Estehlaky.UnitTests.Common;

public class ErrorCollectorTests
{
    private static readonly Error A = Error.Failure("T.A", "a");
    private static readonly Error B = Error.Failure("T.B", "b");

    [Fact]
    public void A_new_collector_is_empty_and_converts_to_success()
    {
        var collector = new ErrorCollector();

        Assert.False(collector.HasErrors);
        Assert.Equal(0, collector.Count);
        Assert.True(collector.ToResult().IsSuccess);
    }

    [Fact]
    public void Added_errors_end_up_in_one_failed_result()
    {
        var result = new ErrorCollector().Add(A).Add(B).ToResult();

        Assert.True(result.IsFailure);
        Assert.Equal(new[] { A, B }, result.Errors);
    }

    [Fact]
    public void AddIf_only_adds_when_the_condition_is_true()
    {
        var collector = new ErrorCollector()
            .AddIf(false, A)
            .AddIf(true, B);

        Assert.Equal(new[] { B }, collector.Errors);
    }

    [Fact]
    public void AddIfFailed_copies_errors_from_failures_and_ignores_successes()
    {
        var collector = new ErrorCollector()
            .AddIfFailed(Result.Success())
            .AddIfFailed(Result.Failure(new[] { A, B }));

        Assert.Equal(2, collector.Count);
    }

    [Fact]
    public void Generic_ToResult_only_builds_the_value_on_success()
    {
        var called = false;

        var failed = new ErrorCollector().Add(A).ToResult(() => { called = true; return 1; });
        var ok = new ErrorCollector().ToResult(() => 99);

        Assert.False(called);
        Assert.True(failed.IsFailure);
        Assert.Equal(99, ok.Value);
    }
}
