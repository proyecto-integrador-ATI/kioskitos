using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kioskitos.API.Tests;

public sealed class ExceptionHandlingTests(
    KioskitosWebApplicationFactory factory)
    : IClassFixture<KioskitosWebApplicationFactory>
{
    [Fact]
    public async Task UnexpectedException_ReturnsGenericProblemDetails()
    {
        using var failingFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");

            builder.ConfigureTestServices(services =>
            {
                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(new ThrowingFilter());
                });
            });
        });

        using var client = failingFactory.CreateClient();

        using var response = await client.GetAsync("/api/health");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(body);

        Assert.Equal(
            500,
            document.RootElement.GetProperty("status").GetInt32());

        Assert.Equal(
            "Internal server error",
            document.RootElement.GetProperty("title").GetString());

        Assert.DoesNotContain("private test details", body);
    }

    private sealed class ThrowingFilter : IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            throw new InvalidOperationException(
                "private test details");
        }
    }
}