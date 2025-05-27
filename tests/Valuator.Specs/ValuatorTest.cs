using Microsoft.AspNetCore.Mvc.Testing;

namespace Valuator.Specs;

public class ValuatorTest
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ValuatorTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public void Test1()
    {
        HttpClient client = _factory.CreateClient();
    }
}
    