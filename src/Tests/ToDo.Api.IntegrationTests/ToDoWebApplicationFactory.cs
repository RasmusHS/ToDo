using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Testcontainers.PostgreSql;
using ToDo.Persistence;

namespace ToDo.Api.IntegrationTests;

public class ToDoWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18")
        .WithName($"todo.db{Guid.NewGuid():N}")
        .WithDatabase("todo")
        .WithUsername("sa")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    // Track if THIS factory's database has been initialized
    private bool _databaseInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services
                .SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<ToDoDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
                services.RemoveAll<IDbContextOptionsConfiguration<ToDoDbContext>>();
            }

            services.AddDbContext<ToDoDbContext>(options =>
            {
                options
                    .UseNpgsql(_dbContainer.GetConnectionString())
                    .UseSnakeCaseNamingConvention()
                    .ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            });
        });
    }

    public async Task EnsureDatabaseCreatedAsync(ToDoDbContext context)
    {
        if (!_databaseInitialized)
        {
            await _initLock.WaitAsync();
            try
            {
                if (!_databaseInitialized)
                {
                    await context.Database.EnsureCreatedAsync();
                    _databaseInitialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }
    }

    public Task InitializeAsync()
    {
        return _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
