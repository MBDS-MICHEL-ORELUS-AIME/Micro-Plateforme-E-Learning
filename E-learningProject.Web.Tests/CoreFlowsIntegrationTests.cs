using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace E_learningProject.Web.Tests;

public class CoreFlowsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CoreFlowsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Quiz_Start_ShouldRedirectToLogin_WhenAnonymous()
    {
        var response = await _client.GetAsync("/Quiz/Start/1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Progression_Details_ShouldRedirectToLogin_WhenAnonymous()
    {
        var response = await _client.GetAsync("/Courses/Details/1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Certificates_Verify_ShouldReturnValid_WhenCodeExists()
    {
        var response = await _client.GetAsync("/Certificates/Verify?code=CERT-INT-001");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Certificat valide", content);
        Assert.Contains("CERT-INT-001", content);
    }
}
