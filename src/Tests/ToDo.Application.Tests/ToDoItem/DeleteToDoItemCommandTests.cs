using Helpers;
using Microsoft.EntityFrameworkCore;
using ToDo.Application.CQRS.Commands.ToDoItem.Handlers;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.Tests.ToDoItem;

public class DeleteToDoItemCommandTests : IDisposable
{
    private readonly DbContextOptions<ToDoDbContext> _options;
    private readonly ToDoDbContext _dbContext;
    private readonly DeleteToDoItemCommandHandler _sut;

    public DeleteToDoItemCommandTests()
    {
        _options = new DbContextOptionsBuilder<ToDoDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;

        _dbContext = new ToDoDbContext(_options);
        _sut = new DeleteToDoItemCommandHandler(_dbContext);

    }

    public void Dispose() => _dbContext.Dispose();

    // ---Helpers---

    /// <summary>
    /// Seeds one list with items through a separate context so the handler's context starts with nothing tracked.
    /// </summary>
    private List<ToDoItemEntity> SeedItems(int count = 1)
    {
        using var seedContext = new ToDoDbContext(_options);

        var list = new ToDoListEntity(StringRandom.GetRandomString(20), null);
        seedContext.ToDoLists.Add(list);

        var items = Enumerable.Range(0, count)
            .Select(_ => new ToDoItemEntity(list.Id, StringRandom.GetRandomString(50), null))
            .ToList();
        seedContext.ToDoItems.AddRange(items);

        seedContext.SaveChanges();
        return items;
    }

    private ToDoItemEntity SeedItem() => SeedItems().Single();

    private ToDoDbContext NewAssertContext() => new(_options);

    // ---Happy path---

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsSuccess()
    {
        var item = SeedItem();

        var result = await _sut.DeleteAsync(item.Id);

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesItem()
    {
        var item = SeedItem();

        await _sut.DeleteAsync(item.Id);

        using var assertContext = NewAssertContext();
        Assert.False(await assertContext.ToDoItems.AnyAsync(i => i.Id == item.Id));
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DoesNotAffectSiblingItems()
    {
        var items = SeedItems(count: 3);

        await _sut.DeleteAsync(items[0].Id);

        using var assertContext = NewAssertContext();
        var remainingIds = await assertContext.ToDoItems.Select(i => i.Id).ToListAsync();
        Assert.Equal(items.Skip(1).Select(i => i.Id).Order(), remainingIds.Order());
    }

    /// <summary>
    /// Deleting a dependent must never touch its principal.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ExistingId_KeepsParentList()
    {
        var item = SeedItem();

        await _sut.DeleteAsync(item.Id);

        using var assertContext = NewAssertContext();
        Assert.True(await assertContext.ToDoLists.AnyAsync(l => l.Id == item.ToDoListId));
    }

    [Fact]
    public async Task DeleteAsync_LastItemInList_LeavesEmptyList()
    {
        var item = SeedItem();

        await _sut.DeleteAsync(item.Id);

        using var assertContext = NewAssertContext();
        var list = await assertContext.ToDoLists
            .Include(l => l.ToDoItems)
            .SingleAsync(l => l.Id == item.ToDoListId);
        Assert.Empty(list.ToDoItems);
    }

    [Fact]
    public async Task DeleteAsync_SameIdTwice_SecondReturnsNotFound()
    {
        var item = SeedItem();

        await _sut.DeleteAsync(item.Id);

        using var secondContext = new ToDoDbContext(_options);
        var result = await new DeleteToDoItemCommandHandler(secondContext).DeleteAsync(item.Id);

        Assert.True(result.IsT1);
        Assert.Equal("entity.not.found", Assert.Single(result.AsT1).ErrorCode);
    }

    // ---Not found---

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsNotFoundError()
    {
        SeedItem();
        var missingId = Guid.NewGuid();

        var result = await _sut.DeleteAsync(missingId);

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.not.found", error.ErrorCode);
        Assert.Contains(missingId.ToString(), error.ErrorMessage);
    }

    [Fact]
    public async Task DeleteAsync_EmptyGuid_ReturnsNotFoundError()
    {
        SeedItem();

        var result = await _sut.DeleteAsync(Guid.Empty);

        Assert.True(result.IsT1);
        Assert.Equal("entity.not.found", Assert.Single(result.AsT1).ErrorCode);
    }

    /// <summary>
    /// A list ID is not an item ID; guards against a lookup on the wrong DbSet,
    /// which here would mean deleting a whole list.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ListIdInsteadOfItemId_ReturnsNotFoundAndDeletesNothing()
    {
        var item = SeedItem();

        var result = await _sut.DeleteAsync(item.ToDoListId);

        Assert.True(result.IsT1);
        Assert.Equal("entity.not.found", Assert.Single(result.AsT1).ErrorCode);
        using var assertContext = NewAssertContext();
        Assert.True(await assertContext.ToDoLists.AnyAsync(l => l.Id == item.ToDoListId));
        Assert.True(await assertContext.ToDoItems.AnyAsync(i => i.Id == item.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotRemoveAnything()
    {
        var items = SeedItems(count: 2);

        await _sut.DeleteAsync(Guid.NewGuid());

        using var assertContext = NewAssertContext();
        Assert.Equal(2, await assertContext.ToDoItems.CountAsync());
    }
}
