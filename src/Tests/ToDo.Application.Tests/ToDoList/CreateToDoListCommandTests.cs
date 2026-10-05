using AutoMapper;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using ToDo.Application.CQRS.Commands.ToDoList.Handlers;
using ToDo.Application.DTO.Commands.ToDoList;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.Tests.ToDoList;

public class CreateToDoListCommandTests : IDisposable
{
    private const int TitleLength = 200;
    private const int DescriptionLength = 500;

    // One named in-memory store per test; xUnit creates a new class instance per test.
    private readonly DbContextOptions<ToDoDbContext> _options;
    private readonly ToDoDbContext _dbContext;
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreateToDoListCommandHandler _sut;

    public CreateToDoListCommandTests()
    {
        _options = new DbContextOptionsBuilder<ToDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ToDoDbContext(_options);
        _sut = new CreateToDoListCommandHandler(_dbContext, _mapper.Object);

    }

    public void Dispose() => _dbContext.Dispose();

    // ---Helpers---

    private static CreateToDoListDto CreateDto(string? title = null) => new()
    {
        ListTitle = title ?? StringRandom.GetRandomString(TitleLength),
        ListDescription = StringRandom.GetRandomString(DescriptionLength)
    };

    /// <summary>
    /// Wires the mapper for the happy path: dto -> entity, entity -> response.
    /// </summary>
    private (ToDoListEntity Entity, ToDoListResponseDto Response) SetupMapper(CreateToDoListDto dto)
    {
        var entity = new ToDoListEntity(dto.ListTitle, dto.ListDescription);
        var response = new ToDoListResponseDto();

        _mapper.Setup(m => m.Map<ToDoListEntity>(dto)).Returns(entity);
        _mapper.Setup(m => m.Map<ToDoListResponseDto>(entity)).Returns(response);

        return (entity, response);
    }

    private void SeedList(string title)
    {
        using var seedContext = new ToDoDbContext(_options);
        seedContext.ToDoLists.Add(new ToDoListEntity(title, null));
        seedContext.SaveChanges();
    }

    /// <summary>
    /// Fresh context so assertions read from the store, not the handler's change tracker.
    /// </summary>
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
    public async Task CreateAsync_ValidDto_PersistsList()
    {
        var dto = CreateDto();
        var (entity, _) = SetupMapper(dto);

        await _sut.CreateAsync(dto);

        using var assertContext = NewAssertContext();
        var saved = await assertContext.ToDoLists.SingleAsync();
        Assert.Equal(entity.Id, saved.Id);
        Assert.Equal(dto.ListTitle, saved.ListTitle);
        Assert.Equal(dto.ListDescription, saved.ListDescription);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_MapsResponseFromSavedEntity()
    {
        var dto = CreateDto();
        var (entity, _) = SetupMapper(dto);

        await _sut.CreateAsync(dto);

        _mapper.Verify(m => m.Map<ToDoListEntity>(dto), Times.Once);
        _mapper.Verify(m => m.Map<ToDoListResponseDto>(entity), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OtherListExistsWithDifferentTitle_Succeeds()
    {
        SeedList(StringRandom.GetRandomString(TitleLength));
        var dto = CreateDto();
        SetupMapper(dto);

        var result = await _sut.CreateAsync(dto);

        Assert.True(result.IsT0);
        using var assertContext = NewAssertContext();
        Assert.Equal(2, await assertContext.ToDoLists.CountAsync());
    }

    // ---Duplicate title---

    [Fact]
    public async Task CreateAsync_DuplicateTitle_ReturnsAlreadyExistsError()
    {
        var dto = CreateDto();
        SeedList(dto.ListTitle);

        var result = await _sut.CreateAsync(dto);

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.already.exists", error.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_DuplicateTitle_DoesNotPersist()
    {
        var dto = CreateDto();
        SeedList(dto.ListTitle);

        await _sut.CreateAsync(dto);

        using var assertContext = NewAssertContext();
        Assert.Equal(1, await assertContext.ToDoLists.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DuplicateTitle_DoesNotMap()
    {
        var dto = CreateDto();
        SeedList(dto.ListTitle);

        await _sut.CreateAsync(dto);

        _mapper.VerifyNoOtherCalls();
    }

    // ---Exception path---

    [Fact]
    public async Task CreateAsync_MapperThrows_ReturnsInvalidDataError()
    {
        var dto = CreateDto();
        _mapper.Setup(m => m.Map<ToDoListEntity>(dto)).Throws<InvalidOperationException>();

        var result = await _sut.CreateAsync(dto);

        Assert.True(result.IsT1);
        var error = Assert.Single(result.AsT1);
        Assert.Equal("entity.invalid.data", error.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_MapperThrows_DoesNotPersist()
    {
        var dto = CreateDto();
        _mapper.Setup(m => m.Map<ToDoListEntity>(dto)).Throws<InvalidOperationException>();

        await _sut.CreateAsync(dto);

        using var assertContext = NewAssertContext();
        Assert.Empty(assertContext.ToDoLists);
    }

}
