using AutoMapper;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using ToDo.Application.CQRS.Queries.ToDoList.Handlers;
using ToDo.Application.DTO.Queries;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.Tests.ToDoList;

public class GetToDoListQueryHandlerTests : IDisposable
{
    private readonly DbContextOptions<ToDoDbContext> _options;
    private readonly ToDoDbContext _dbContext;
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetToDoListQueryHandler _sut;

    public GetToDoListQueryHandlerTests()
    {
        _options = new DbContextOptionsBuilder<ToDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ToDoDbContext(_options);
        _sut = new GetToDoListQueryHandler(_dbContext, _mapper.Object);

    }

    public void Dispose() => _dbContext.Dispose();

    // ---Helpers---

    /// <summary>
    /// Seeds through a separate context so the handler's context has nothing tracked;
    /// items only show up if the handler's Include actually loads them.
    /// </summary>
    private Guid SeedList(int itemCount)
    {
        using var seedContext = new ToDoDbContext(_options);

        var list = new ToDoListEntity(StringRandom.GetRandomString(20), null);
        seedContext.ToDoLists.Add(list);

        for (var i = 0; i < itemCount; i++)
        {
            seedContext.ToDoItems.Add(new ToDoItemEntity(list.Id, StringRandom.GetRandomString(50), null));
        }

        seedContext.SaveChanges();
        return list.Id;
    }

    private QueryToDoListDto SetupMapper()
    {
        var response = new QueryToDoListDto();
        _mapper.Setup(m => m.Map<QueryToDoListDto>(It.IsAny<ToDoListEntity>())).Returns(response);
        return response;
    }

    // ---Found---

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsMappedDto()
    {
        var listId = SeedList(itemCount: 0);
        var response = SetupMapper();

        var result = await _sut.GetAsync(listId);

        Assert.True(result.IsT0);
        Assert.Same(response, result.AsT0);
    }

    [Fact]
    public async Task GetAsync_ExistingId_MapsRequestedList()
    {
        var listId = SeedList(itemCount: 0);
        SeedList(itemCount: 0); // decoy
        SetupMapper();

        await _sut.GetAsync(listId);

        _mapper.Verify(m => m.Map<QueryToDoListDto>(
            It.Is<ToDoListEntity>(e => e.Id == listId)), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ListWithItems_IncludesItems()
    {
        var listId = SeedList(itemCount: 3);
        SetupMapper();

        await _sut.GetAsync(listId);

        _mapper.Verify(m => m.Map<QueryToDoListDto>(
            It.Is<ToDoListEntity>(e => e.ToDoItems.Count == 3)), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ListWithItems_IncludesOnlyItsOwnItems()
    {
        var listId = SeedList(itemCount: 2);
        SeedList(itemCount: 5); // decoy with more items
        SetupMapper();

        await _sut.GetAsync(listId);

        _mapper.Verify(m => m.Map<QueryToDoListDto>(
            It.Is<ToDoListEntity>(e =>
                e.ToDoItems.Count == 2 &&
                e.ToDoItems.All(i => i.ToDoListId == listId))), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ListWithoutItems_MapsEmptyItemCollection()
    {
        var listId = SeedList(itemCount: 0);
        SetupMapper();

        await _sut.GetAsync(listId);

        _mapper.Verify(m => m.Map<QueryToDoListDto>(
            It.Is<ToDoListEntity>(e => e.ToDoItems != null && e.ToDoItems.Count == 0)), Times.Once);
    }

    // ---Not found---

    [Fact]
    public async Task GetAsync_NonExistentId_ReturnsNotFoundError()
    {
        SeedList(itemCount: 0);
        var missingId = Guid.NewGuid();

        var result = await _sut.GetAsync(missingId);

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.not.found", error.ErrorCode);
        Assert.Contains(missingId.ToString(), error.ErrorMessage);
    }

    [Fact]
    public async Task GetAsync_EmptyGuid_ReturnsNotFoundError()
    {
        SeedList(itemCount: 0);

        var result = await _sut.GetAsync(Guid.Empty);

        Assert.True(result.IsT1);
        Assert.Equal("entity.not.found", Assert.Single(result.AsT1).ErrorCode);
    }

    [Fact]
    public async Task GetAsync_NonExistentId_DoesNotMap()
    {
        await _sut.GetAsync(Guid.NewGuid());

        _mapper.VerifyNoOtherCalls();
    }

    // ---Exception path---

    [Fact]
    public async Task GetAsync_MapperThrows_ReturnsInvalidDataError()
    {
        var listId = SeedList(itemCount: 0);
        _mapper.Setup(m => m.Map<QueryToDoListDto>(It.IsAny<ToDoListEntity>()))
            .Throws<InvalidOperationException>();

        var result = await _sut.GetAsync(listId);

        Assert.True(result.IsT1);
        Assert.Equal("entity.invalid.data", Assert.Single(result.AsT1).ErrorCode);
    }

}
