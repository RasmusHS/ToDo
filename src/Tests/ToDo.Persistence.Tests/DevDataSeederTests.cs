using Microsoft.EntityFrameworkCore;
using ToDo.Domain;
using ToDo.Persistence.Seeding;

namespace ToDo.Persistence.Tests;

[Collection(PostgresCollection.Name)]
public sealed class DevDataSeederTests(PostgresFixture fixture) : IAsyncLifetime
{
    // xUnit creates a new instance per test, so every test starts on empty tables.
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SeedAsync_EmptyDatabase_SeedsListsAndItems()
    {
        await SeedAsync();

        var (lists, items) = await CountAsync();
        Assert.True(lists > 0);
        Assert.True(items > 0);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicate()
    {
        await SeedAsync();
        var afterFirst = await CountAsync();

        await SeedAsync();

        Assert.Equal(afterFirst, await CountAsync());
    }

    [Fact]
    public async Task SeedAsync_DatabaseHasData_DoesNothing()
    {
        var existing = new ToDoListEntity("Existing", null);
        await using (var ctx = fixture.CreateContext())
        {
            ctx.Add(existing);
            await ctx.SaveChangesAsync();
        }

        await SeedAsync();

        await using var assertCtx = fixture.CreateContext();
        var only = Assert.Single(await assertCtx.Set<ToDoListEntity>().ToListAsync());
        Assert.Equal(existing.Id, only.Id);
        Assert.False(await assertCtx.Set<ToDoItemEntity>().AnyAsync());
    }

    [Fact]
    public async Task Seed_Sync_SeedsAndIsIdempotent()
    {
        await using (var ctx = fixture.CreateContext())
            DevDataSeeder.Seed(ctx);
        var afterFirst = await CountAsync();

        await using (var ctx = fixture.CreateContext())
            DevDataSeeder.Seed(ctx);

        Assert.True(afterFirst.Lists > 0);
        Assert.Equal(afterFirst, await CountAsync());
    }

    // Fresh context per call so assertions hit the database, not the change tracker.
    private async Task SeedAsync()
    {
        await using var ctx = fixture.CreateContext();
        await DevDataSeeder.SeedAsync(ctx, CancellationToken.None);
    }

    private async Task<(int Lists, int Items)> CountAsync()
    {
        await using var ctx = fixture.CreateContext();
        return (await ctx.Set<ToDoListEntity>().CountAsync(),
                await ctx.Set<ToDoItemEntity>().CountAsync());
    }

}
