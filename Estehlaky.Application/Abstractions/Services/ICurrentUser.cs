namespace Estehlaky.Application.Abstractions.Services;

public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsAuthenticated { get; }
}
