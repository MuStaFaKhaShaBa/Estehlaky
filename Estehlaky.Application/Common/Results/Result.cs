namespace Estehlaky.Application.Common.Results;

public class Result : IResult<Result>
{
    private static readonly IReadOnlyList<Error> NoErrors = [];
    private readonly IReadOnlyList<Error> _errors;

    protected Result(bool isSuccess, IReadOnlyList<Error>? errors)
    {
        if (!isSuccess && (errors is null || errors.Count == 0))
            throw new ArgumentException("A failed result needs at least one error.", nameof(errors));

        IsSuccess = isSuccess;
        _errors = errors ?? NoErrors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    /// <summary>All errors. Empty when the result is successful, never null.</summary>
    public IReadOnlyList<Error> Errors => _errors;

    /// <summary>The first (primary) error. Throws when the result is successful.</summary>
    public Error Error => IsFailure
        ? _errors[0]
        : throw new InvalidOperationException("Result is successful; it has no error.");

    // ---- creation ----
    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, [error]);
    public static Result Failure(IEnumerable<Error> errors) => new(false, errors.ToList());

    public static Result<T> Success<T>(T value) => Result<T>.Ok(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Fail([error]);
    public static Result<T> Failure<T>(IEnumerable<Error> errors) => Result<T>.Fail(errors.ToList());

    /// <summary>Merges the errors of every failed result into one failed result.</summary>
    public static Result Combine(IEnumerable<Result> results)
    {
        var errors = results.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToList();
        return errors.Count == 0 ? Success() : Failure(errors);
    }

    public static Result Combine(params Result[] results) =>
        Combine((IEnumerable<Result>)results);

    public static implicit operator Result(Error error) => Failure(error);

    static Result IResult<Result>.CreateFailure(Error error) => Failure(error);
    static Result IResult<Result>.CreateFailure(IEnumerable<Error> errors) => Failure(errors);
}

public sealed class Result<T> : Result, IResult<Result<T>>
{
    private readonly T? _value;

    private Result(T? value, IReadOnlyList<Error>? errors, bool isSuccess)
        : base(isSuccess, errors) => _value = value;

    internal static Result<T> Ok(T value) => new(value, null, true);
    internal static Result<T> Fail(IReadOnlyList<Error> errors) => new(default, errors, false);

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Result failed; it has no value.");

    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail([error]);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsFailure ? Result<TOut>.Fail(Errors) : Result<TOut>.Ok(map(Value));

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Errors);

    static Result<T> IResult<Result<T>>.CreateFailure(Error error) => Fail([error]);
    static Result<T> IResult<Result<T>>.CreateFailure(IEnumerable<Error> errors) => Fail(errors.ToList());
}
