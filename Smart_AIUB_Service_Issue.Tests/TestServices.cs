using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Tests;

/// <summary>
/// Builds a small DI container like Program.cs does, but backed by an in-memory
/// SQLite database. Each instance gets its own fresh, empty database.
/// </summary>
public sealed class TestServices : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public IServiceScope Scope { get; }

    private TestServices(SqliteConnection connection, ServiceProvider provider)
    {
        _connection = connection;
        _provider = provider;
        Scope = provider.CreateScope();
    }

    public T Get<T>() where T : notnull => Scope.ServiceProvider.GetRequiredService<T>();

    public static async Task<TestServices> CreateAsync(Action<IServiceCollection>? configure = null)
    {
        // The in-memory database lives as long as this connection stays open.
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection(); // needed by the password-reset token provider
        // Anyone asking for ApplicationDbContext gets a TestDbContext (see that class).
        services.AddDbContext<ApplicationDbContext, TestDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>(IdentityConfig.Configure)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        configure?.Invoke(services);

        var testServices = new TestServices(connection, services.BuildServiceProvider());

        await testServices.Get<ApplicationDbContext>().Database.EnsureCreatedAsync();

        var roleManager = testServices.Get<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        return testServices;
    }

    public async ValueTask DisposeAsync()
    {
        Scope.Dispose();
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
