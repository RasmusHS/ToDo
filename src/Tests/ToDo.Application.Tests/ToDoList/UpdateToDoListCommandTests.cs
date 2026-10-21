using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using ToDo.Application.CQRS.Commands.ToDoList.Handlers;
using ToDo.Application.DTO.Commands.ToDoList;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.Tests.ToDoList;

[ExcludeFromCodeCoverage]
public class UpdateToDoListCommandTests : IDisposable
{
    private const int TitleLength = 200;
    private const int DescriptionLength = 500;

    private readonly DbContextOptions<ToDoDbContext> _options;
    private readonly ToDoDbContext _dbContext;
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateToDoListCommandHandler _sut;

    public UpdateToDoListCommandTests()
    {
        _options = new DbContextOptionsBuilder<ToDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ToDoDbContext(_options);
        _sut = new UpdateToDoListCommandHandler(_dbContext, _mapper.Object);

    }

    public void Dispose() => _dbContext.Dispose();

    // ---Helpers---

    private static string RandomTitle() => StringRandom.GetRandomString(TitleLength);
    private static string RandomDescription() => StringRandom.GetRandomString(DescriptionLength);

    /// <summary>
    /// Seeds through a separate context so the handler's context starts with nothing tracked.
    /// </summary>
    private ToDoListEntity SeedList(string? title = null, int itemCount = 0)
    {
        using var seedContext = new ToDoDbContext(_options);

        var list = new ToDoListEntity(title ?? RandomTitle(), RandomDescription());
        seedContext.ToDoLists.Add(list);

        for (var i = 0; i < itemCount; i++)
        {
            seedContext.ToDoItems.Add(new ToDoItemEntity(list.Id, StringRandom.GetRandomString(50), null));
        }

        seedContext.SaveChanges();
        return list;
    }

    private static UpdateToDoListDto CreateDto(Guid id, string? title = null, string? description = null) => new()
    {
        Id = id,
        ListTitle = title ?? RandomTitle(),
        ListDescription = description ?? RandomDescription()
    };

    private ToDoListResponseDto SetupMapper()
    {
        var response = new ToDoListResponseDto();
        _mapper.Setup(m => m.Map<ToDoListResponseDto>(It.IsAny<ToDoListEntity>())).Returns(response);
        return response;
    }

    private ToDoDbContext NewAssertContext() => new(_options);

    // ---Happy path---

    [Fact]
    public async Task UpdateAsync_ValidDto_ReturnsMappedResponse()
    {
        var list = SeedList();
        var response = SetupMapper();

        var result = await _sut.UpdateAsync(CreateDto(list.Id));

        Assert.True(result.IsT0);
        Assert.Same(response, result.AsT0);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_PersistsChanges()
    {
        var list = SeedList();
        SetupMapper();
        var dto = CreateDto(list.Id);

        await _sut.UpdateAsync(dto);

        using var assertContext = NewAssertContext();
        var saved = await assertContext.ToDoLists.SingleAsync();
        Assert.Equal(dto.ListTitle, saved.ListTitle);
        Assert.Equal(dto.ListDescription, saved.ListDescription);
    }

    [Fact]
    public async Task UpdateAsync_NullDescription_ClearsDescription()
    {
        var list = SeedList();
        SetupMapper();
        var dto = CreateDto(list.Id);
        dto.ListDescription = null;

        await _sut.UpdateAsync(dto);

        using var assertContext = NewAssertContext();
        Assert.Null((await assertContext.ToDoLists.SingleAsync()).ListDescription);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_RefreshesModifiedOnAndKeepsIdAndCreatedOn()
    {
        var list = SeedList();
        SetupMapper();

        var before = DateTime.UtcNow;
        await _sut.UpdateAsync(CreateDto(list.Id));
        var after = DateTime.UtcNow;

        using var assertContext = NewAssertContext();
        var saved = await assertContext.ToDoLists.SingleAsync();
        Assert.Equal(list.Id, saved.Id);
        Assert.Equal(list.CreatedOn, saved.CreatedOn);
        Assert.InRange(saved.ModifiedOn, before, after);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_MapsUpdatedEntity()
    {
        var list = SeedList();
        SetupMapper();
        var dto = CreateDto(list.Id);

        await _sut.UpdateAsync(dto);

        _mapper.Verify(m => m.Map<ToDoListResponseDto>(
            It.Is<ToDoListEntity>(e => e.Id == list.Id && e.ListTitle == dto.ListTitle)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_DoesNotAffectOtherLists()
    {
        var target = SeedList();
        var other = SeedList();
        SetupMapper();

        await _sut.UpdateAsync(CreateDto(target.Id));

        using var assertContext = NewAssertContext();
        var untouched = await assertContext.ToDoLists.SingleAsync(l => l.Id == other.Id);
        Assert.Equal(other.ListTitle, untouched.ListTitle);
        Assert.Equal(other.ListDescription, untouched.ListDescription);
        Assert.Equal(other.ModifiedOn, untouched.ModifiedOn);
    }

    [Fact]
    public async Task UpdateAsync_ListWithItems_KeepsItems()
    {
        var list = SeedList(itemCount: 3);
        SetupMapper();

        await _sut.UpdateAsync(CreateDto(list.Id));

        using var assertContext = NewAssertContext();
        Assert.Equal(3, await assertContext.ToDoItems.CountAsync(i => i.ToDoListId == list.Id));
    }

    /// <summary>
    /// The duplicate check must exclude the list being updated, or this rejects a description-only edit.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_SameTitleNewDescription_Succeeds()
    {
        var list = SeedList();
        SetupMapper();
        var dto = CreateDto(list.Id, title: list.ListTitle);

        var result = await _sut.UpdateAsync(dto);

        Assert.True(result.IsT0);
        using var assertContext = NewAssertContext();
        Assert.Equal(dto.ListDescription, (await assertContext.ToDoLists.SingleAsync()).ListDescription);
    }

    // ---Not found---

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNotFoundError()
    {
        SeedList();
        var missingId = Guid.NewGuid();

        var result = await _sut.UpdateAsync(CreateDto(missingId));

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.not.found", error.ErrorCode);
        Assert.Contains(missingId.ToString(), error.ErrorMessage);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_DoesNotMap()
    {
        await _sut.UpdateAsync(CreateDto(Guid.NewGuid()));

        _mapper.VerifyNoOtherCalls();
    }

    // ---Title taken by another list---

    [Fact]
    public async Task UpdateAsync_TitleTakenByOtherList_ReturnsAlreadyExistsError()
    {
        var target = SeedList();
        var other = SeedList();

        var result = await _sut.UpdateAsync(CreateDto(target.Id, title: other.ListTitle));

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.already.exists", error.ErrorCode);
        Assert.Contains(other.ListTitle, error.ErrorMessage);
    }

    [Fact]
    public async Task UpdateAsync_TitleTakenByOtherList_DoesNotPersist()
    {
        var target = SeedList();
        var other = SeedList();

        await _sut.UpdateAsync(CreateDto(target.Id, title: other.ListTitle));

        using var assertContext = NewAssertContext();
        var unchanged = await assertContext.ToDoLists.SingleAsync(l => l.Id == target.Id);
        Assert.Equal(target.ListTitle, unchanged.ListTitle);
        Assert.Equal(target.ListDescription, unchanged.ListDescription);
        Assert.Equal(target.ModifiedOn, unchanged.ModifiedOn);
    }

    [Fact]
    public async Task UpdateAsync_TitleTakenByOtherList_DoesNotMap()
    {
        var target = SeedList();
        var other = SeedList();

        await _sut.UpdateAsync(CreateDto(target.Id, title: other.ListTitle));

        _mapper.VerifyNoOtherCalls();
    }

    // ---Both---

    /// <summary>
    /// Not-found short-circuits: a title conflict on a list that doesn't exist isn't reported.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NonExistentIdAndTakenTitle_ReturnsOnlyNotFound()
    {
        var other = SeedList();

        var result = await _sut.UpdateAsync(CreateDto(Guid.NewGuid(), title: other.ListTitle));

        Assert.True(result.IsT1);
        Assert.Equal("entity.not.found", Assert.Single(result.AsT1).ErrorCode);
    }

    // ---Exceptions---

    [Fact]
    public async Task UpdateAsync_MapperThrows_ExceptionPropagates()
    {
        var list = SeedList();
        _mapper.Setup(m => m.Map<ToDoListResponseDto>(It.IsAny<ToDoListEntity>()))
            .Throws<InvalidOperationException>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateAsync(CreateDto(list.Id)));
    }

}
