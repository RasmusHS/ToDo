using AutoMapper;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using ToDo.Application.CQRS.Commands.ToDoItem.Handlers;
using ToDo.Application.DTO.Commands.ToDoItem;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.Tests.ToDoItem;

public class CreateToDoItemCommandTests : IDisposable
{
    private const int TextLength = 50;
    private const int StatusLength = 15;

    private readonly DbContextOptions<ToDoDbContext> _options;
    private readonly ToDoDbContext _dbContext;
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreateToDoItemCommandHandler _sut;

    // Parent list seeded per test so the item has a real owner.
    private readonly Guid _listId;

    public CreateToDoItemCommandTests()
    {
        _options = new DbContextOptionsBuilder<ToDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _listId = SeedList();

        _dbContext = new ToDoDbContext(_options);
        _sut = new CreateToDoItemCommandHandler(_dbContext, _mapper.Object);

    }

    public void Dispose() => _dbContext.Dispose();

    // ---Helpers---

    private Guid SeedList()
    {
        using var seedContext = new ToDoDbContext(_options);
        var list = new ToDoListEntity(StringRandom.GetRandomString(20), null);
        seedContext.ToDoLists.Add(list);
        seedContext.SaveChanges();
        return list.Id;
    }

    private CreateToDoItemDto CreateDto(bool withStatus = true) => new()
    {
        ToDoListId = _listId,
        Text = StringRandom.GetRandomString(TextLength),
        Status = withStatus ? StringRandom.GetRandomString(StatusLength) : null
    };

    private (ToDoItemEntity Entity, ToDoItemResponseDto Response) SetupMapper(CreateToDoItemDto dto)
    {
        var entity = new ToDoItemEntity(dto.ToDoListId, dto.Text, dto.Status);
        var response = new ToDoItemResponseDto();

        _mapper.Setup(m => m.Map<ToDoItemEntity>(dto)).Returns(entity);
        _mapper.Setup(m => m.Map<ToDoItemResponseDto>(entity)).Returns(response);

        return (entity, response);
    }

    private ToDoDbContext NewAssertContext() => new(_options);

    // ---Happy path---

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsMappedResponse()
    {
        var dto = CreateDto();
        var (_, response) = SetupMapper(dto);

        var result = await _sut.CreateAsync(dto);

        Assert.True(result.IsT0);
        Assert.Same(response, result.AsT0);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_PersistsItem()
    {
        var dto = CreateDto();
        var (entity, _) = SetupMapper(dto);

        await _sut.CreateAsync(dto);

        using var assertContext = NewAssertContext();
        var saved = await assertContext.ToDoItems.SingleAsync();
        Assert.Equal(entity.Id, saved.Id);
        Assert.Equal(_listId, saved.ToDoListId);
        Assert.Equal(dto.Text, saved.Text);
        Assert.Equal(dto.Status, saved.Status);
        Assert.False(saved.IsDone);
    }

    [Fact]
    public async Task CreateAsync_NullStatus_PersistsWithoutStatus()
    {
        var dto = CreateDto(withStatus: false);
        SetupMapper(dto);

        var result = await _sut.CreateAsync(dto);

        Assert.True(result.IsT0);
        using var assertContext = NewAssertContext();
        var saved = await assertContext.ToDoItems.SingleAsync();
        Assert.Null(saved.Status);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ItemReachableFromParentList()
    {
        var dto = CreateDto();
        var (entity, _) = SetupMapper(dto);

        await _sut.CreateAsync(dto);

        using var assertContext = NewAssertContext();
        var list = await assertContext.ToDoLists
            .Include(l => l.ToDoItems)
            .SingleAsync(l => l.Id == _listId);
        var item = Assert.Single(list.ToDoItems);
        Assert.Equal(entity.Id, item.Id);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_MapsResponseFromSavedEntity()
    {
        var dto = CreateDto();
        var (entity, _) = SetupMapper(dto);

        await _sut.CreateAsync(dto);

        _mapper.Verify(m => m.Map<ToDoItemEntity>(dto), Times.Once);
        _mapper.Verify(m => m.Map<ToDoItemResponseDto>(entity), Times.Once);
    }

    // ---Exception path---

    [Fact]
    public async Task CreateAsync_MapperThrows_ReturnsInvalidDataError()
    {
        var dto = CreateDto();
        _mapper.Setup(m => m.Map<ToDoItemEntity>(dto)).Throws<InvalidOperationException>();

        var result = await _sut.CreateAsync(dto);

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.invalid.data", error.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_MapperThrows_DoesNotPersist()
    {
        var dto = CreateDto();
        _mapper.Setup(m => m.Map<ToDoItemEntity>(dto)).Throws<InvalidOperationException>();

        await _sut.CreateAsync(dto);

        using var assertContext = NewAssertContext();
        Assert.Empty(assertContext.ToDoItems);
    }

}
