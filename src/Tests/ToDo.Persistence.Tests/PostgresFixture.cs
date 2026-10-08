using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using ToDo.Domain;

namespace ToDo.Persistence.Tests;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18")
        .WithName($"todo.db{Guid.NewGuid():N}")
        .WithDatabase("todo")
        .WithUsername("sa")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    /// <summary>
    /// Exposed so tests can drive AddPersistence with a real configuration.
    /// </summary>
    public string ConnectionString => _dbContainer.GetConnectionString();


    public ToDoDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ToDoDbContext>()
        .UseNpgsql(_dbContainer.GetConnectionString())
        .UseSnakeCaseNamingConvention()
        .Options);

    /// <summary>
    /// Empties all tables. Items first because of the FK to lists.
    /// Uses ExecuteDelete so no table names are hardcoded.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var ctx = CreateContext();
        await ctx.Set<ToDoItemEntity>().ExecuteDeleteAsync();
        await ctx.Set<ToDoListEntity>().ExecuteDeleteAsync();
    }


    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _dbContainer.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

