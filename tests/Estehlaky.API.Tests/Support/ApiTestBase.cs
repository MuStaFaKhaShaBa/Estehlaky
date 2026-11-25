namespace Estehlaky.API.Tests.Support;

public abstract class ApiTestBase : IDisposable
{
    protected ApiFixture Api { get; }
    protected ApiClient Client { get; }

    protected ApiTestBase(ApiFixture api)
    {
        Api = api;
        Client = api.CreateClient();
    }

    public void Dispose()
    {
        Api.Clock.Reset();   // never leak a moved clock into the next test
        Client.Dispose();
    }
}
