using System.Net;
using System.Net.Http.Json;
using Helpers;
using Microsoft.EntityFrameworkCore;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoList;
using ToDo.Domain;

namespace ToDo.Api.IntegrationTests;

public class ToDoListControllerTest : BaseIntegrationTest
{
    private const string PostUrl = "api/ToDoList/postToDoList";

    // Alphabetic so random data can't trip a character rule in the validator.
    private const int TitleLength = 200;
    private const int DescriptionLength = 500;

    private readonly HttpClient _client;

    public ToDoListControllerTest(ToDoWebApplicationFactory factory) : base(factory)
    {
        _client = factory.CreateClient();
    }

    // ---Helpers---

    private static CreateToDoListDto CreateDto(string? title = null, bool withDescription = true) => new()
    {
        ListTitle = title ?? StringRandom.GetRandomAlphabeticString(TitleLength),
        ListDescription = withDescription ? StringRandom.GetRandomAlphabeticString(DescriptionLength) : null
    };

    private async Task SeedListAsync(string title)
    {
        DbContext.ToDoLists.Add(new ToDoListEntity(title, null));
        await DbContext.SaveChangesAsync();
    }

    // AsNoTracking: the request runs in its own scope, so read the database, not this context's tracker.
    private IQueryable<ToDoListEntity> Lists => DbContext.ToDoLists.AsNoTracking();

    // ---Happy path---

    [Fact]
    public async Task PostToDoList_ValidDto_Returns200WithCreatedList()
    {
        var dto = CreateDto();

        var response = await _client.PostAsJsonAsync(PostUrl, dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ToDoListResponseDto>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(dto.ListTitle, body.ListTitle);
        Assert.Equal(dto.ListDescription, body.ListDescription);
    }

    [Fact]
    public async Task PostToDoList_ValidDto_PersistsList()
    {
        var dto = CreateDto();

        var response = await _client.PostAsJsonAsync(PostUrl, dto);
        var body = await response.Content.ReadFromJsonAsync<ToDoListResponseDto>();

        var saved = await Lists.SingleAsync();
        Assert.Equal(body!.Id, saved.Id);
        Assert.Equal(dto.ListTitle, saved.ListTitle);
        Assert.Equal(dto.ListDescription, saved.ListDescription);
    }

    /// <summary>
    /// Body is mapped from the in-memory entity (100 ns ticks); Postgres stores microseconds.
    /// Compare body vs. database with a tolerance, never exact equality.
    /// </summary>
    [Fact]
    public async Task PostToDoList_ValidDto_ReturnsTimestampsMatchingDatabase()
    {
        var before = DateTime.UtcNow;
        var response = await _client.PostAsJsonAsync(PostUrl, CreateDto());
        var after = DateTime.UtcNow;

        var body = await response.Content.ReadFromJsonAsync<ToDoListResponseDto>();
        var saved = await Lists.SingleAsync();

        Assert.InRange(body!.CreatedOn, before, after);
        Assert.Equal(body.CreatedOn, body.ModifiedOn);
        Assert.Equal(body.CreatedOn, saved.CreatedOn, TimeSpan.FromMilliseconds(1));
        Assert.Equal(body.ModifiedOn, saved.ModifiedOn, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task PostToDoList_NullDescription_Returns200AndPersistsNull()
    {
        var dto = CreateDto(withDescription: false);

        var response = await _client.PostAsJsonAsync(PostUrl, dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await Lists.SingleAsync();
        Assert.Null(saved.ListDescription);
    }

    // ---Duplicate title---

    [Fact]
    public async Task PostToDoList_DuplicateTitle_Returns400WithAlreadyExistsError()
    {
        var dto = CreateDto();
        await SeedListAsync(dto.ListTitle);

        var response = await _client.PostAsJsonAsync(PostUrl, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>();
        var error = Assert.Single(errors!);
        Assert.Equal("entity.already.exists", error.ErrorCode);
    }

    [Fact]
    public async Task PostToDoList_DuplicateTitle_DoesNotPersist()
    {
        var dto = CreateDto();
        await SeedListAsync(dto.ListTitle);

        await _client.PostAsJsonAsync(PostUrl, dto);

        Assert.Equal(1, await Lists.CountAsync());
    }

    /// <summary>
    /// Real Postgres: default collation is case-sensitive, so this is NOT a duplicate.
    /// Flip the assertion if the group decides titles are case-insensitive.
    /// </summary>
    [Fact]
    public async Task PostToDoList_SameTitleDifferentCase_IsNotDuplicate()
    {
        var title = StringRandom.GetRandomAlphabeticString(TitleLength);
        await SeedListAsync(title.ToLowerInvariant());

        var response = await _client.PostAsJsonAsync(PostUrl, CreateDto(title.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, await Lists.CountAsync());
    }

    // ---Validation---

    [Fact]
    public async Task PostToDoList_EmptyTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync(PostUrl, CreateDto(title: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostToDoList_EmptyTitle_DoesNotPersist()
    {
        await _client.PostAsJsonAsync(PostUrl, CreateDto(title: ""));

        Assert.Empty(await Lists.ToListAsync());
    }

}
