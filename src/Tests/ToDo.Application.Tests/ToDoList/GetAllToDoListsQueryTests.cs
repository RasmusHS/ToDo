using AutoMapper;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using ToDo.Application.CQRS.Queries.ToDoList.Handlers;
using ToDo.Application.DTO.Queries;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.Tests.ToDoList;

public class GetAllToDoListsQueryTests : IDisposable
{
    private readonly DbContextOptions<ToDoDbContext> _options;
    private readonly ToDoDbContext _dbContext;
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetAllToDoListsQueryHandler _sut;

    public GetAllToDoListsQueryTests()
    {
        _options = new DbContextOptionsBuilder<ToDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ToDoDbContext(_options);
        _sut = new GetAllToDoListsQueryHandler(_dbContext, _mapper.Object);

    }

    public void Dispose() => _dbContext.Dispose();

    // ---Helpers---

    /// <summary>
    /// Seeds through a separate context so the handler's context starts with nothing tracked.
    /// </summary>
    private List<Guid> SeedLists(int count, int itemsPerList = 0)
    {
        using var seedContext = new ToDoDbContext(_options);
        var ids = new List<Guid>();

        for (var i = 0; i < count; i++)
        {
            var list = new ToDoListEntity(StringRandom.GetRandomString(20), null);
            seedContext.ToDoLists.Add(list);
            ids.Add(list.Id);

            for (var j = 0; j < itemsPerList; j++)
            {
                seedContext.ToDoItems.Add(new ToDoItemEntity(list.Id, StringRandom.GetRandomString(50), null));
            }
        }

        seedContext.SaveChanges();
        return ids;
    }

    private List<QueryToDoListDto> SetupMapper()
    {
        var response = new List<QueryToDoListDto>();
        _mapper.Setup(m => m.Map<List<QueryToDoListDto>>(It.IsAny<List<ToDoListEntity>>()))
            .Returns(response);
        return response;
    }

    // ---Lists exist---

    [Fact]
    public async Task GetAllAsync_ListsExist_ReturnsMappedDtos()
    {
        SeedLists(count: 2);
        var response = SetupMapper();

        var result = await _sut.GetAllAsync();

        Assert.True(result.IsT0);
        Assert.Same(response, result.AsT0);
    }

    /// <summary>
    /// No ORDER BY in the query, so compare as sets, not sequences.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ListsExist_MapsEveryList()
    {
        var ids = SeedLists(count: 3);
        SetupMapper();

        await _sut.GetAllAsync();

        _mapper.Verify(m => m.Map<List<QueryToDoListDto>>(
            It.Is<List<ToDoListEntity>>(l =>
                l.Count == 3 &&
                l.Select(e => e.Id).ToHashSet().SetEquals(ids))), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_SingleList_ReturnsSuccess()
    {
        SeedLists(count: 1);
        SetupMapper();

        var result = await _sut.GetAllAsync();

        Assert.True(result.IsT0);
    }

    /// <summary>
    /// Documents current behavior: no Include, so items are not loaded. Because the entity
    /// initializes ToDoItems to an empty list, an unloaded collection looks identical to an
    /// empty one. Flip this if the query gets .Include(l => l.ToDoItems).
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ListsWithItems_DoesNotLoadItems()
    {
        SeedLists(count: 2, itemsPerList: 3);
        SetupMapper();

        await _sut.GetAllAsync();

        _mapper.Verify(m => m.Map<List<QueryToDoListDto>>(
            It.Is<List<ToDoListEntity>>(l => l.All(e => e.ToDoItems.Count == 0))), Times.Once);
    }

    // ---No lists---
    // Pins current behavior (error on empty). If empty becomes a successful [], replace these
    // with a test asserting IsT0 and an empty result.

    [Fact]
    public async Task GetAllAsync_NoLists_ReturnsNotFoundCollectionError()
    {
        var result = await _sut.GetAllAsync();

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.collection.not.found", error.ErrorCode);
    }

    [Fact]
    public async Task GetAllAsync_NoLists_DoesNotMap()
    {
        await _sut.GetAllAsync();

        _mapper.VerifyNoOtherCalls();
    }

    // ---Exceptions---

    /// <summary>
    /// No catch-all: unexpected failures propagate to the global exception handler.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_MapperThrows_ExceptionPropagates()
    {
        SeedLists(count: 1);
        _mapper.Setup(m => m.Map<List<QueryToDoListDto>>(It.IsAny<List<ToDoListEntity>>()))
            .Throws<InvalidOperationException>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetAllAsync());
    }

}
