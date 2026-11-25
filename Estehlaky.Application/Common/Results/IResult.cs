namespace Estehlaky.Application.Common.Results;

/// <summary>
/// Lets generic code (pipeline behaviors) create a failed result of any result type
/// without reflection.
/// </summary>
public interface IResult<TSelf> where TSelf : IResult<TSelf>
{
    static abstract TSelf CreateFailure(Error error);
    static abstract TSelf CreateFailure(IEnumerable<Error> errors);
}
