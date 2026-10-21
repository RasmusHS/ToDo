using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using Helpers;
using Microsoft.EntityFrameworkCore;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoList;
using ToDo.Domain;

namespace ToDo.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public class ToDoListControllerPutTests : BaseIntegrationTest
{
    private const string PostUrl = "api/ToDoList/postToDoList";

    // Alphabetic so random data can't trip a character rule in the validator.
    private const int TitleLength = 200;
    private const int DescriptionLength = 500;

    private readonly HttpClient _client;

    public ToDoListControllerPutTests(ToDoWebApplicationFactory factory) : base(factory)
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

    // ===PutToDoList===

    private const string PutUrl = "api/ToDoList/putToDoList";

    /// <summary>
    /// Seeds and returns the row as stored, so timestamps carry Postgres' microsecond precision.
    /// </summary>
    private async Task<ToDoListEntity> SeedStoredListAsync(int itemCount = 0)
    {
        var list = new ToDoListEntity(
            StringRandom.GetRandomAlphabeticString(TitleLength),
            StringRandom.GetRandomAlphabeticString(DescriptionLength));
        DbContext.ToDoLists.Add(list);

        for (var i = 0; i < itemCount; i++)
        {
            DbContext.ToDoItems.Add(new ToDoItemEntity(list.Id, StringRandom.GetRandomAlphabeticString(50), null));
        }

        await DbContext.SaveChangesAsync();
        return await Lists.SingleAsync(l => l.Id == list.Id);
    }

    private static UpdateToDoListDto UpdateDto(Guid id, string? title = null, bool withDescription = true) => new()
    {
        Id = id,
        ListTitle = title ?? StringRandom.GetRandomAlphabeticString(TitleLength),
        ListDescription = withDescription ? StringRandom.GetRandomAlphabeticString(DescriptionLength) : null
    };

    // ---Happy path---

    [Fact]
    public async Task PutToDoList_ValidDto_Returns200WithUpdatedList()
    {
        var list = await SeedStoredListAsync();
        var dto = UpdateDto(list.Id);

        var response = await _client.PutAsJsonAsync(PutUrl, dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ToDoListResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(list.Id, body.Id);
        Assert.Equal(dto.ListTitle, body.ListTitle);
        Assert.Equal(dto.ListDescription, body.ListDescription);
    }

    [Fact]
    public async Task PutToDoList_ValidDto_PersistsChanges()
    {
        var list = await SeedStoredListAsync();
        var dto = UpdateDto(list.Id);

        await _client.PutAsJsonAsync(PutUrl, dto);

        var saved = await Lists.SingleAsync();
        Assert.Equal(dto.ListTitle, saved.ListTitle);
        Assert.Equal(dto.ListDescription, saved.ListDescription);
    }

    [Fact]
    public async Task PutToDoList_ValidDto_RefreshesModifiedOnAndKeepsCreatedOn()
    {
        var list = await SeedStoredListAsync();

        var before = DateTime.UtcNow;
        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(list.Id));
        var after = DateTime.UtcNow;

        var body = await response.Content.ReadFromJsonAsync<ToDoListResponseDto>();
        var saved = await Lists.SingleAsync();

        Assert.Equal(list.CreatedOn, saved.CreatedOn);
        Assert.Equal(list.CreatedOn, body!.CreatedOn);
        Assert.InRange(body.ModifiedOn, before, after);
        Assert.True(saved.ModifiedOn > list.ModifiedOn);
        // Body from in-memory entity (100 ns ticks), row stored with microseconds.
        Assert.Equal(body.ModifiedOn, saved.ModifiedOn, TimeSpan.FromMilliseconds(1));
    }

    /// <summary>
    /// Regression: the duplicate-title check must not match the list being updated.
    /// </summary>
    [Fact]
    public async Task PutToDoList_SameTitleNewDescription_Returns200()
    {
        var list = await SeedStoredListAsync();
        var dto = UpdateDto(list.Id, title: list.ListTitle);

        var response = await _client.PutAsJsonAsync(PutUrl, dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(dto.ListDescription, (await Lists.SingleAsync()).ListDescription);
    }

    [Fact]
    public async Task PutToDoList_NullDescription_ClearsDescription()
    {
        var list = await SeedStoredListAsync();

        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(list.Id, withDescription: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null((await Lists.SingleAsync()).ListDescription);
    }

    [Fact]
    public async Task PutToDoList_ListWithItems_KeepsItems()
    {
        var list = await SeedStoredListAsync(itemCount: 3);

        await _client.PutAsJsonAsync(PutUrl, UpdateDto(list.Id));

        Assert.Equal(3, await DbContext.ToDoItems.AsNoTracking().CountAsync(i => i.ToDoListId == list.Id));
    }

    // ---Not found---
    // Controller maps every handler error to 400. If not-found becomes 404, update the status here.

    [Fact]
    public async Task PutToDoList_NonExistentId_Returns400WithNotFoundError()
    {
        await SeedStoredListAsync();
        var missingId = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(missingId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>();
        var error = Assert.Single(errors!);
        Assert.Equal("entity.not.found", error.ErrorCode);
        Assert.Contains(missingId.ToString(), error.ErrorMessage);
    }

    [Fact]
    public async Task PutToDoList_NonExistentId_DoesNotCreateList()
    {
        await _client.PutAsJsonAsync(PutUrl, UpdateDto(Guid.NewGuid()));

        Assert.Empty(await Lists.ToListAsync());
    }

    // ---Title taken by another list---

    [Fact]
    public async Task PutToDoList_TitleTakenByOtherList_Returns400WithAlreadyExistsError()
    {
        var target = await SeedStoredListAsync();
        var other = await SeedStoredListAsync();

        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(target.Id, title: other.ListTitle));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>();
        Assert.Equal("entity.already.exists", Assert.Single(errors!).ErrorCode);
    }

    [Fact]
    public async Task PutToDoList_TitleTakenByOtherList_DoesNotPersist()
    {
        var target = await SeedStoredListAsync();
        var other = await SeedStoredListAsync();

        await _client.PutAsJsonAsync(PutUrl, UpdateDto(target.Id, title: other.ListTitle));

        var unchanged = await Lists.SingleAsync(l => l.Id == target.Id);
        Assert.Equal(target.ListTitle, unchanged.ListTitle);
        Assert.Equal(target.ListDescription, unchanged.ListDescription);
        Assert.Equal(target.ModifiedOn, unchanged.ModifiedOn);
    }

    // ---Validation---

    [Fact]
    public async Task PutToDoList_EmptyTitle_Returns400()
    {
        var list = await SeedStoredListAsync();

        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(list.Id, title: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutToDoList_EmptyTitle_DoesNotPersist()
    {
        var list = await SeedStoredListAsync();

        await _client.PutAsJsonAsync(PutUrl, UpdateDto(list.Id, title: ""));

        var unchanged = await Lists.SingleAsync();
        Assert.Equal(list.ListTitle, unchanged.ListTitle);
        Assert.Equal(list.ModifiedOn, unchanged.ModifiedOn);
    }

}
