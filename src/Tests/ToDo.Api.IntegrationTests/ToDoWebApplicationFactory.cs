using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Testcontainers.PostgreSql;
using ToDo.Persistence;

namespace ToDo.Api.IntegrationTests;

public class ToDoWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:latest")
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
            }

            services.AddDbContext<ToDoDbContext>(options =>
            {
                options
                    .UseNpgsql(_dbContainer.GetConnectionString())//+ ";DefaultConnection=todo.db"
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

    public new Task DisposeAsync() => Task.CompletedTask;

    //Task IAsyncLifetime.DisposeAsync()
    //{
    //    throw new NotImplementedException();
    //}
}
