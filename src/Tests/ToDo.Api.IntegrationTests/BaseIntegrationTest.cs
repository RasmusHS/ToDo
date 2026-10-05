using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToDo.Persistence;

namespace ToDo.Api.IntegrationTests;

public abstract class BaseIntegrationTest : IClassFixture<ToDoWebApplicationFactory>, IDisposable
{
    private readonly IServiceScope _scope;
    private readonly ToDoWebApplicationFactory _factory;
    protected readonly ToDoDbContext DbContext;

    protected BaseIntegrationTest(ToDoWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
        InitializeDatabaseAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeDatabaseAsync()
    {
        // Factory ensures database is created once per container
        await _factory.EnsureDatabaseCreatedAsync(DbContext);

        // Clean data for this test
        await CleanDatabaseAsync();
    }

    private async Task CleanDatabaseAsync()
    {
        // Remove all data from tables
        //DbContext.ToDoLists.RemoveRange(DbContext.ToDoLists);
        await DbContext.ToDoLists.ExecuteDeleteAsync();

        await DbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        DbContext?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (DbContext != null)
            await DbContext.DisposeAsync();

        _scope?.Dispose();
    }
}
