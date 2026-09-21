using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Tests;

public class DatabaseReadyMiddlewareTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _factory;

    public DatabaseReadyMiddlewareTests(AppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WhenDatabaseNotReady_RedirectsToHelpPage()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var state = _factory.Services.GetRequiredService<AppStartupState>();
        state.MarkFailed("ทดสอบ SQL ไม่พร้อม");

        try
        {
            var response = await client.GetAsync("/");
            Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Home/Database", response.Headers.Location?.ToString());

            var help = await client.GetAsync("/Home/Database");
            help.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await help.Content.ReadAsStringAsync());
            Assert.Contains("ทดสอบ SQL ไม่พร้อม", html);
            Assert.Contains("ฐานข้อมูลยังไม่พร้อม", html);
        }
        finally
        {
            state.MarkReady();
        }
    }
}
