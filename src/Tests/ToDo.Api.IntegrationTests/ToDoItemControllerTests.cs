using System.Net;
using System.Net.Http.Json;
using Helpers;
using Microsoft.EntityFrameworkCore;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoItem;
using ToDo.Domain;

namespace ToDo.Api.IntegrationTests;

public class ToDoItemControllerTests : BaseIntegrationTest
{
    private const string PostUrl = "api/ToDoItem/postToDoItem";

    private const int TextLength = 50;
    private const int StatusLength = 15;

    private readonly HttpClient _client;

    public ToDoItemControllerTests(ToDoWebApplicationFactory factory) : base(factory)
    {
        _client = factory.CreateClient();
    }

    // ---Helpers---

    private async Task<Guid> SeedListAsync()
    {
        var list = new ToDoListEntity(StringRandom.GetRandomAlphabeticString(20), null);
        DbContext.ToDoLists.Add(list);
        await DbContext.SaveChangesAsync();
        return list.Id;
    }

    private static CreateToDoItemDto CreateDto(Guid listId, string? text = null, bool withStatus = true) => new()
    {
        ToDoListId = listId,
        Text = text ?? StringRandom.GetRandomAlphabeticString(TextLength),
        Status = withStatus ? StringRandom.GetRandomAlphabeticString(StatusLength) : null
    };

    private IQueryable<ToDoItemEntity> Items => DbContext.ToDoItems.AsNoTracking();

    // ---Happy path---

    [Fact]
    public async Task PostToDoItem_ValidDto_Returns200WithCreatedItem()
    {
        var listId = await SeedListAsync();
        var dto = CreateDto(listId);

        var response = await _client.PostAsJsonAsync(PostUrl, dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ToDoItemResponseDto>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(listId, body.ToDoListId);
        Assert.Equal(dto.Text, body.Text);
        Assert.Equal(dto.Status, body.Status);
        Assert.False(body.IsDone);
    }

    [Fact]
    public async Task PostToDoItem_ValidDto_PersistsItem()
    {
        var listId = await SeedListAsync();
        var dto = CreateDto(listId);

        var response = await _client.PostAsJsonAsync(PostUrl, dto);
        var body = await response.Content.ReadFromJsonAsync<ToDoItemResponseDto>();

        var saved = await Items.SingleAsync();
        Assert.Equal(body!.Id, saved.Id);
        Assert.Equal(listId, saved.ToDoListId);
        Assert.Equal(dto.Text, saved.Text);
        Assert.Equal(dto.Status, saved.Status);
        Assert.False(saved.IsDone);
    }

    [Fact]
    public async Task PostToDoItem_ValidDto_ItemReachableFromParentList()
    {
        var listId = await SeedListAsync();

        var response = await _client.PostAsJsonAsync(PostUrl, CreateDto(listId));
        var body = await response.Content.ReadFromJsonAsync<ToDoItemResponseDto>();

        var list = await DbContext.ToDoLists
            .AsNoTracking()
            .Include(l => l.ToDoItems)
            .SingleAsync(l => l.Id == listId);
        var item = Assert.Single(list.ToDoItems);
        Assert.Equal(body!.Id, item.Id);
    }

    /// <summary>
    /// Body is mapped from the in-memory entity (100 ns ticks); Postgres stores microseconds.
    /// </summary>
    [Fact]
    public async Task PostToDoItem_ValidDto_ReturnsTimestampsMatchingDatabase()
    {
        var listId = await SeedListAsync();

        var before = DateTime.UtcNow;
        var response = await _client.PostAsJsonAsync(PostUrl, CreateDto(listId));
        var after = DateTime.UtcNow;

        var body = await response.Content.ReadFromJsonAsync<ToDoItemResponseDto>();
        var saved = await Items.SingleAsync();

        Assert.InRange(body!.CreatedOn, before, after);
        Assert.Equal(body.CreatedOn, body.ModifiedOn);
        Assert.Equal(body.CreatedOn, saved.CreatedOn, TimeSpan.FromMilliseconds(1));
        Assert.Equal(body.ModifiedOn, saved.ModifiedOn, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task PostToDoItem_NullStatus_Returns200AndPersistsNull()
    {
        var listId = await SeedListAsync();

        var response = await _client.PostAsJsonAsync(PostUrl, CreateDto(listId, withStatus: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await Items.SingleAsync();
        Assert.Null(saved.Status);
    }

    [Fact]
    public async Task PostToDoItem_MultipleItemsSameList_AllPersisted()
    {
        var listId = await SeedListAsync();

        await _client.PostAsJsonAsync(PostUrl, CreateDto(listId));
        await _client.PostAsJsonAsync(PostUrl, CreateDto(listId));

        Assert.Equal(2, await Items.CountAsync(i => i.ToDoListId == listId));
    }

    // ---Non-existent list---
    // Current behavior: FK violation -> DbUpdateException -> handler catch-all -> InvalidData.
    // If the handler gets an explicit list-exists check, change the expected code to "entity.not.found".

    [Fact]
    public async Task PostToDoItem_NonExistentList_Returns400()
    {
        var response = await _client.PostAsJsonAsync(PostUrl, CreateDto(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>();
        var error = Assert.Single(errors!);
        Assert.Equal("entity.invalid.data", error.ErrorCode);
    }

    [Fact]
    public async Task PostToDoItem_NonExistentList_DoesNotPersist()
    {
        await _client.PostAsJsonAsync(PostUrl, CreateDto(Guid.NewGuid()));

        Assert.Empty(await Items.ToListAsync());
    }

    // ---Validation---
    // Status only: body may be ValidationProblemDetails (implicit [Required]) rather than ErrorResponseDto.

    [Fact]
    public async Task PostToDoItem_EmptyText_Returns400()
    {
        var listId = await SeedListAsync();

        var response = await _client.PostAsJsonAsync(PostUrl, CreateDto(listId, text: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostToDoItem_EmptyText_DoesNotPersist()
    {
        var listId = await SeedListAsync();

        await _client.PostAsJsonAsync(PostUrl, CreateDto(listId, text: ""));

        Assert.Empty(await Items.ToListAsync());
    }

}
