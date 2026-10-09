using AutoMapper;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ToDo.Application.CQRS.Queries.ToDoList.Handlers;
using ToDo.Application.DTO.Queries;
using ToDo.Application.Errors;
using ToDo.Application.Profiles;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.Tests.ToDoList;

public class GetAllToDoListsQueryTests : IDisposable
{
    private readonly DbContextOptions<ToDoDbContext> _options;
    private readonly ToDoDbContext _dbContext;
    private readonly GetAllToDoListsQueryHandler _sut;

    public GetAllToDoListsQueryTests()
    {
        _options = new DbContextOptionsBuilder<ToDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ToDoDbContext(_options);
        _sut = new GetAllToDoListsQueryHandler(_dbContext, CreateMapper());

    }

    public void Dispose() => _dbContext.Dispose();

    // ---Helpers---

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance)
        .CreateMapper();

    /// <summary>
    /// Seeds through a separate context so the handler's context starts with nothing tracked.
    /// </summary>
    private List<ToDoListEntity> SeedLists(params int[] itemsPerList)
    {
        using var seedContext = new ToDoDbContext(_options);
        var lists = new List<ToDoListEntity>();

        foreach (var itemCount in itemsPerList)
        {
            var list = new ToDoListEntity(StringRandom.GetRandomString(20), null);
            seedContext.ToDoLists.Add(list);
            lists.Add(list);

            for (var j = 0; j < itemCount; j++)
                seedContext.ToDoItems.Add(new ToDoItemEntity(list.Id, StringRandom.GetRandomString(50), null));
        }

        seedContext.SaveChanges();
        return lists;
    }

    // ---Lists exist---

    [Fact]
    public async Task GetAllAsync_SingleList_ReturnsSuccess()
    {
        SeedLists(0);

        var result = await _sut.GetAllAsync();

        Assert.True(result.IsT0);
    }

    /// <summary>
    /// No ORDER BY in the query, so compare as sets, not sequences.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ListsExist_ReturnsEveryList()
    {
        var seeded = SeedLists(0, 0, 0);

        var result = await _sut.GetAllAsync();

        var ids = result.AsT0.Select(d => d.Id).ToHashSet();
        Assert.True(ids.SetEquals(seeded.Select(l => l.Id)));
    }

    [Fact]
    public async Task GetAllAsync_ListsExist_MapsFields()
    {
        var seeded = Assert.Single(SeedLists(0));

        var result = await _sut.GetAllAsync();

        var dto = Assert.Single(result.AsT0);
        Assert.Equal(seeded.Id, dto.Id);
        Assert.Equal(seeded.ListTitle, dto.ListTitle);
        Assert.Equal(seeded.ListDescription, dto.ListDescription);
        Assert.Equal(seeded.CreatedOn, dto.CreatedOn);
        Assert.Equal(seeded.ModifiedOn, dto.ModifiedOn);
    }

    [Fact]
    public async Task GetAllAsync_ListsWithItems_CountsItemsPerList()
    {
        var seeded = SeedLists(0, 1, 3);

        var result = await _sut.GetAllAsync();

        var counts = result.AsT0.ToDictionary(d => d.Id, d => d.ItemCount);
        Assert.Equal(0, counts[seeded[0].Id]);
        Assert.Equal(1, counts[seeded[1].Id]);
        Assert.Equal(3, counts[seeded[2].Id]);
    }

    // ---No lists---
    // Pins current behavior (error on empty). If empty becomes a successful [], replace this
    // with a test asserting IsT0 and an empty result.

    [Fact]
    public async Task GetAllAsync_NoLists_ReturnsNotFoundCollectionError()
    {
        var result = await _sut.GetAllAsync();

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        var expected = ToDoListErrors.NotFoundCollection();
        Assert.Equal(expected.ErrorCode, error.ErrorCode);
        Assert.Equal(expected.ErrorMessage, error.ErrorMessage);
    }

    // ---Exceptions---

    /// <summary>
    /// No catch-all: unexpected failures propagate to the global exception handler.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ContextFails_ExceptionPropagates()
    {
        _dbContext.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => _sut.GetAllAsync());
    }

}
