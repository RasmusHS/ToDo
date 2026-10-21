using System.Diagnostics.CodeAnalysis;
using Helpers;
using Microsoft.EntityFrameworkCore;
using ToDo.Application.CQRS.Commands.ToDoList.Handlers;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.Tests.ToDoList;

[ExcludeFromCodeCoverage]
public class DeleteToDoListCommandTests : IDisposable
{
    private readonly DbContextOptions<ToDoDbContext> _options;
    private readonly ToDoDbContext _dbContext;
    private readonly DeleteToDoListCommandHandler _sut;

    public DeleteToDoListCommandTests()
    {
        _options = new DbContextOptionsBuilder<ToDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ToDoDbContext(_options);
        _sut = new DeleteToDoListCommandHandler(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    // ---Helpers---

    /// <summary>
    /// Seeds through a separate context so the handler's context starts with nothing tracked.
    /// </summary>
    private ToDoListEntity SeedList()
    {
        using var seedContext = new ToDoDbContext(_options);
        var list = new ToDoListEntity(StringRandom.GetRandomString(20), StringRandom.GetRandomString(100));
        seedContext.ToDoLists.Add(list);
        seedContext.SaveChanges();
        return list;
    }

    private ToDoDbContext NewAssertContext() => new(_options);

    // ---Happy path---
    // Item cascade isn't tested here: InMemory has no FK cascade. Covered by the API tests on Postgres.

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsSuccess()
    {
        var list = SeedList();

        var result = await _sut.DeleteAsync(list.Id);

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesList()
    {
        var list = SeedList();

        await _sut.DeleteAsync(list.Id);

        using var assertContext = NewAssertContext();
        Assert.False(await assertContext.ToDoLists.AnyAsync(l => l.Id == list.Id));
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DoesNotAffectOtherLists()
    {
        var target = SeedList();
        var other = SeedList();

        await _sut.DeleteAsync(target.Id);

        using var assertContext = NewAssertContext();
        Assert.Equal(other.Id, (await assertContext.ToDoLists.SingleAsync()).Id);
    }

    [Fact]
    public async Task DeleteAsync_SameIdTwice_SecondReturnsNotFound()
    {
        var list = SeedList();

        await _sut.DeleteAsync(list.Id);

        using var secondContext = new ToDoDbContext(_options);
        var result = await new DeleteToDoListCommandHandler(secondContext).DeleteAsync(list.Id);

        Assert.True(result.IsT1);
        Assert.Equal("entity.not.found", Assert.Single(result.AsT1).ErrorCode);
    }

    // ---Not found---

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsNotFoundError()
    {
        SeedList();
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
        SeedList();

        var result = await _sut.DeleteAsync(Guid.Empty);

        Assert.True(result.IsT1);
        Assert.Equal("entity.not.found", Assert.Single(result.AsT1).ErrorCode);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotRemoveAnything()
    {
        var list = SeedList();

        await _sut.DeleteAsync(Guid.NewGuid());

        using var assertContext = NewAssertContext();
        Assert.Equal(list.Id, (await assertContext.ToDoLists.SingleAsync()).Id);
    }

}
