namespace Estehlaky.Application.Common.Results;

/// <summary>
/// Gathers errors during a handler's checks so all problems are reported in one Result,
/// instead of stopping at the first one.
/// </summary>
public sealed class ErrorCollector
{
    private readonly List<Error> _errors = [];

    public bool HasErrors => _errors.Count > 0;
    public int Count => _errors.Count;
    public IReadOnlyList<Error> Errors => _errors;

    public ErrorCollector Add(Error error)
    {
        _errors.Add(error);
        return this;
    }

    /// <summary>Adds the error only when the condition is true.</summary>
    public ErrorCollector AddIf(bool condition, Error error) =>
        condition ? Add(error) : this;

    /// <summary>Adds every error of a failed result (does nothing for a successful one).</summary>
    public ErrorCollector AddIfFailed(Result result)
    {
        if (result.IsFailure) _errors.AddRange(result.Errors);
        return this;
    }

    public Result ToResult() =>
        HasErrors ? Result.Failure(_errors) : Result.Success();

    public Result<T> ToResult<T>(Func<T> onSuccess) =>
        HasErrors ? Result.Failure<T>(_errors) : Result.Success(onSuccess());
}
