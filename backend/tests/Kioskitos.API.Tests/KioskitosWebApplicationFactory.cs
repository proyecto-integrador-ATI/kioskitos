using Kioskitos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kioskitos.API.Tests;

public sealed class KioskitosWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Each test class gets its own factory instance, but migrations only need to run once per test run.
    private static readonly SemaphoreSlim MigrationLock = new(1, 1);
    private static bool _migrated;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:ClientId"] = "test-google-client-id",
                ["Google:ClientSecret"] = "test-google-client-secret",
                ["Jwt:Key"] = "test-jwt-key-with-at-least-32-characters",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            });
        });
    }

    /// <summary>
    /// Applies pending EF Core migrations when a real database is configured, so
    /// database-backed tests work against a fresh database (e.g. the CI Postgres service).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__Default")))
        {
            return;
        }

        await MigrationLock.WaitAsync();
        try
        {
            if (_migrated)
            {
                return;
            }

            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            _migrated = true;
        }
        finally
        {
            MigrationLock.Release();
        }
    }

    // Disposal is handled by WebApplicationFactory's own IAsyncDisposable implementation.
    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
