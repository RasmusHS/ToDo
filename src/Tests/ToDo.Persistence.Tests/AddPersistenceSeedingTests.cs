using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToDo.Domain;

namespace ToDo.Persistence.Tests;

/// <summary>
/// Verifies the Database:SeedDevData flag wiring in AddPersistence,
/// i.e. that seeding is opt-in and only "true" turns it on.
/// </summary>
[Collection(PostgresCollection.Name)]

public sealed class AddPersistenceSeedingTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    public async Task Migrate_FlagTrue_SeedsDatabase(string flag)
    {
        await MigrateThroughAddPersistenceAsync(flag);

        await using var ctx = fixture.CreateContext();
        Assert.True(await ctx.Set<ToDoListEntity>().AnyAsync());
    }

    [Theory]
    [InlineData("false")]
    [InlineData("not-a-bool")]
    [InlineData("")]
    [InlineData(null)] // key absent
    public async Task Migrate_FlagNotTrue_DoesNotSeed(string? flag)
    {
        await MigrateThroughAddPersistenceAsync(flag);

        await using var ctx = fixture.CreateContext();
        Assert.False(await ctx.Set<ToDoListEntity>().AnyAsync());
    }

    // The schema is already migrated by the fixture; EF still invokes the
    // seeding delegates on Migrate even when no migrations are pending.
    private async Task MigrateThroughAddPersistenceAsync(string? seedFlag)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString,
        };
        if (seedFlag is not null)
            settings["Database:SeedDevData"] = seedFlag;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        await using var provider = new ServiceCollection()
            .AddPersistence(configuration)
            .BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
        await ctx.Database.MigrateAsync();
    }

}
