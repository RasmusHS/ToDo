using AutoMapper;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using ToDo.Application.DTO.Commands.ToDoItem;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.Tests.ToDoItem;

public class UpdateToDoItemCommandTests : IDisposable
{
    private const int TextLength = 500;
    private const int StatusLength = 100;

    private readonly DbContextOptions<ToDoDbContext> _options;
    private readonly ToDoDbContext _dbContext;
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateToDoItemCommandHandler _sut;

    public UpdateToDoItemCommandTests()
    {
        _options = new DbContextOptionsBuilder<ToDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ToDoDbContext(_options);
        _sut = new UpdateToDoItemCommandHandler(_dbContext, _mapper.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    // ---Helpers---

    private static string RandomText() => StringRandom.GetRandomString(TextLength);
    private static string RandomStatus() => StringRandom.GetRandomString(StatusLength);

    /// <summary>
    /// Seeds a list with items through a separate context so the handler's context starts with nothing tracked.
    /// </summary>
    private List<ToDoItemEntity> SeedItems(int count = 1)
    {
        using var seedContext = new ToDoDbContext(_options);

        var list = new ToDoListEntity(StringRandom.GetRandomString(20), null);
        seedContext.ToDoLists.Add(list);

        var items = Enumerable.Range(0, count)
            .Select(_ => new ToDoItemEntity(list.Id, RandomText(), RandomStatus()))
            .ToList();
        seedContext.ToDoItems.AddRange(items);

        seedContext.SaveChanges();
        return items;
    }

    private ToDoItemEntity SeedItem() => SeedItems().Single();

    private static UpdateToDoItemDto CreateDto(ToDoItemEntity item, bool isDone = true, bool withStatus = true) =>
        CreateDto(item.Id, item.ToDoListId, isDone, withStatus);

    private static UpdateToDoItemDto CreateDto(Guid id, Guid listId, bool isDone = true, bool withStatus = true) => new()
    {
        Id = id,
        ToDoListId = listId,
        IsDone = isDone,
        Text = RandomText(),
        Status = withStatus ? RandomStatus() : null
    };

    private ToDoItemResponseDto SetupMapper()
    {
        var response = new ToDoItemResponseDto();
        _mapper.Setup(m => m.Map<ToDoItemResponseDto>(It.IsAny<ToDoItemEntity>())).Returns(response);
        return response;
    }

    private ToDoDbContext NewAssertContext() => new(_options);

    // ---Happy path---

    [Fact]
    public async Task UpdateAsync_ValidDto_ReturnsMappedResponse()
    {
        var item = SeedItem();
        var response = SetupMapper();

        var result = await _sut.UpdateAsync(CreateDto(item));

        Assert.True(result.IsT0);
        Assert.Same(response, result.AsT0);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_PersistsChanges()
    {
        var item = SeedItem();
        SetupMapper();
        var dto = CreateDto(item, isDone: true);

        await _sut.UpdateAsync(dto);

        using var assertContext = NewAssertContext();
        var saved = await assertContext.ToDoItems.SingleAsync();
        Assert.True(saved.IsDone);
        Assert.Equal(dto.Text, saved.Text);
        Assert.Equal(dto.Status, saved.Status);
    }

    [Fact]
    public async Task UpdateAsync_NullStatus_ClearsStatus()
    {
        var item = SeedItem();
        SetupMapper();

        await _sut.UpdateAsync(CreateDto(item, withStatus: false));

        using var assertContext = NewAssertContext();
        Assert.Null((await assertContext.ToDoItems.SingleAsync()).Status);
    }

    /// <summary>
    /// Persisted round trip: done, then undone, across two separate handler calls.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_IsDoneFalseAfterTrue_PersistsUndone()
    {
        var item = SeedItem();
        SetupMapper();

        await _sut.UpdateAsync(CreateDto(item, isDone: true));

        using (var secondContext = new ToDoDbContext(_options))
        {
            var secondSut = new UpdateToDoItemCommandHandler(secondContext, _mapper.Object);
            await secondSut.UpdateAsync(CreateDto(item, isDone: false));
        }

        using var assertContext = NewAssertContext();
        Assert.False((await assertContext.ToDoItems.SingleAsync()).IsDone);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_RefreshesModifiedOnAndKeepsIdentity()
    {
        var item = SeedItem();
        SetupMapper();

        var before = DateTime.UtcNow;
        await _sut.UpdateAsync(CreateDto(item));
        var after = DateTime.UtcNow;

        using var assertContext = NewAssertContext();
        var saved = await assertContext.ToDoItems.SingleAsync();
        Assert.Equal(item.Id, saved.Id);
        Assert.Equal(item.ToDoListId, saved.ToDoListId);
        Assert.Equal(item.CreatedOn, saved.CreatedOn);
        Assert.InRange(saved.ModifiedOn, before, after);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_MapsUpdatedEntity()
    {
        var item = SeedItem();
        SetupMapper();
        var dto = CreateDto(item);

        await _sut.UpdateAsync(dto);

        _mapper.Verify(m => m.Map<ToDoItemResponseDto>(
            It.Is<ToDoItemEntity>(e => e.Id == item.Id && e.Text == dto.Text && e.IsDone == dto.IsDone)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_DoesNotAffectSiblingItems()
    {
        var items = SeedItems(count: 3);
        var target = items[0];
        SetupMapper();

        await _sut.UpdateAsync(CreateDto(target));

        using var assertContext = NewAssertContext();
        foreach (var sibling in items.Skip(1))
        {
            var untouched = await assertContext.ToDoItems.SingleAsync(i => i.Id == sibling.Id);
            Assert.Equal(sibling.Text, untouched.Text);
            Assert.Equal(sibling.Status, untouched.Status);
            Assert.Equal(sibling.IsDone, untouched.IsDone);
            Assert.Equal(sibling.ModifiedOn, untouched.ModifiedOn);
        }
    }

    // ---Not found---

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNotFoundError()
    {
        var item = SeedItem();
        var missingId = Guid.NewGuid();

        var result = await _sut.UpdateAsync(CreateDto(missingId, item.ToDoListId));

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.not.found", error.ErrorCode);
        Assert.Contains(missingId.ToString(), error.ErrorMessage);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_DoesNotPersistOrMap()
    {
        var item = SeedItem();

        await _sut.UpdateAsync(CreateDto(Guid.NewGuid(), item.ToDoListId));

        _mapper.VerifyNoOtherCalls();
        using var assertContext = NewAssertContext();
        var unchanged = await assertContext.ToDoItems.SingleAsync();
        Assert.Equal(item.Text, unchanged.Text);
        Assert.Equal(item.ModifiedOn, unchanged.ModifiedOn);
    }

    /// <summary>
    /// A list ID is not an item ID; guards against a lookup on the wrong DbSet.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ListIdInsteadOfItemId_ReturnsNotFoundError()
    {
        var item = SeedItem();

        var result = await _sut.UpdateAsync(CreateDto(item.ToDoListId, item.ToDoListId));

        Assert.True(result.IsT1);
        Assert.Equal("entity.not.found", Assert.Single(result.AsT1).ErrorCode);
    }

    // ---List mismatch---

    [Fact]
    public async Task UpdateAsync_WrongListId_ReturnsNotFoundInCollectionError()
    {
        var item = SeedItem();

        var result = await _sut.UpdateAsync(CreateDto(item.Id, Guid.NewGuid()));

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.not.found.in.collection", error.ErrorCode);
        Assert.Contains(item.Id.ToString(), error.ErrorMessage);
    }

    /// <summary>
    /// The real case: the item exists, and so does the other list, but the item isn't in it.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ItemFromOtherList_DoesNotPersistOrMap()
    {
        var item = SeedItem();
        var otherListItem = SeedItem(); // seeds a second list

        var result = await _sut.UpdateAsync(CreateDto(item.Id, otherListItem.ToDoListId));

        Assert.True(result.IsT1);
        _mapper.VerifyNoOtherCalls();
        using var assertContext = NewAssertContext();
        var unchanged = await assertContext.ToDoItems.SingleAsync(i => i.Id == item.Id);
        Assert.Equal(item.ToDoListId, unchanged.ToDoListId);
        Assert.Equal(item.Text, unchanged.Text);
        Assert.Equal(item.ModifiedOn, unchanged.ModifiedOn);
    }

    // ---Exceptions---

    [Fact]
    public async Task UpdateAsync_MapperThrows_ExceptionPropagates()
    {
        var item = SeedItem();
        _mapper.Setup(m => m.Map<ToDoItemResponseDto>(It.IsAny<ToDoItemEntity>()))
            .Throws<InvalidOperationException>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateAsync(CreateDto(item)));
    }


}
