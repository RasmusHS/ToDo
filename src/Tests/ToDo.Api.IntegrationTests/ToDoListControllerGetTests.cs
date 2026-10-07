using System.Net;
using System.Net.Http.Json;
using Helpers;
using Microsoft.EntityFrameworkCore;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Queries;
using ToDo.Domain;

namespace ToDo.Api.IntegrationTests;

public class ToDoListControllerGetTests : BaseIntegrationTest
{
    private static string GetUrl(object id) => $"api/ToDoList/getToDoList/{id}";

    private readonly HttpClient _client;

    public ToDoListControllerGetTests(ToDoWebApplicationFactory factory) : base(factory)
    {
        _client = factory.CreateClient();
    }

    // ---Helpers---

    private async Task<ToDoListEntity> SeedListAsync(int itemCount = 0)
    {
        var list = new ToDoListEntity(
            StringRandom.GetRandomAlphabeticString(20),
            StringRandom.GetRandomAlphabeticString(100));
        DbContext.ToDoLists.Add(list);

        for (var i = 0; i < itemCount; i++)
        {
            DbContext.ToDoItems.Add(new ToDoItemEntity(
                list.Id, StringRandom.GetRandomAlphabeticString(50), null));
        }

        await DbContext.SaveChangesAsync();
        return list;
    }

    // ---Found---

    [Fact]
    public async Task GetToDoList_ExistingId_Returns200WithList()
    {
        var list = await SeedListAsync();

        var response = await _client.GetAsync(GetUrl(list.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<QueryToDoListDto>();
        Assert.NotNull(body);
        Assert.Equal(list.Id, body.Id);
        Assert.Equal(list.ListTitle, body.ListTitle);
        Assert.Equal(list.ListDescription, body.ListDescription);
    }

    /// <summary>
    /// Unlike POST, the body here is mapped from a row read back from Postgres,
    /// so it must match the stored timestamps exactly.
    /// </summary>
    [Fact]
    public async Task GetToDoList_ExistingId_ReturnsStoredTimestamps()
    {
        var list = await SeedListAsync();
        var saved = await DbContext.ToDoLists.AsNoTracking().SingleAsync(l => l.Id == list.Id);

        var body = await (await _client.GetAsync(GetUrl(list.Id))).Content.ReadFromJsonAsync<QueryToDoListDto>();

        Assert.Equal(saved.CreatedOn, body!.CreatedOn);
        Assert.Equal(saved.ModifiedOn, body.ModifiedOn);
    }

    [Fact]
    public async Task GetToDoList_ListWithoutItems_ReturnsEmptyItems()
    {
        var list = await SeedListAsync(itemCount: 0);

        var body = await (await _client.GetAsync(GetUrl(list.Id))).Content.ReadFromJsonAsync<QueryToDoListDto>();

        Assert.NotNull(body!.ToDoItems);
        Assert.Empty(body.ToDoItems);
    }

    [Fact]
    public async Task GetToDoList_ListWithItems_ReturnsItsItems()
    {
        var list = await SeedListAsync(itemCount: 3);
        var expectedIds = await DbContext.ToDoItems.AsNoTracking()
            .Where(i => i.ToDoListId == list.Id)
            .Select(i => i.Id)
            .ToListAsync();

        var body = await (await _client.GetAsync(GetUrl(list.Id))).Content.ReadFromJsonAsync<QueryToDoListDto>();

        Assert.NotNull(body?.ToDoItems);
        Assert.Equal(expectedIds.Order(), body.ToDoItems.Select(i => i.Id).Order());
        Assert.All(body.ToDoItems, i => Assert.Equal(list.Id, i.ToDoListId));
    }

    [Fact]
    public async Task GetToDoList_OtherListsExist_ReturnsOnlyRequestedList()
    {
        var list = await SeedListAsync(itemCount: 2);
        await SeedListAsync(itemCount: 4); // decoy

        var body = await (await _client.GetAsync(GetUrl(list.Id))).Content.ReadFromJsonAsync<QueryToDoListDto>();

        Assert.Equal(list.Id, body!.Id);
        Assert.NotNull(body.ToDoItems);
        Assert.Equal(2, body.ToDoItems.Count);
    }

    // ---Not found---

    [Fact]
    public async Task GetToDoList_NonExistentId_Returns404WithNotFoundError()
    {
        await SeedListAsync();
        var missingId = Guid.NewGuid();

        var response = await _client.GetAsync(GetUrl(missingId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>();
        var error = Assert.Single(errors!);
        Assert.Equal("entity.not.found", error.ErrorCode);
        Assert.Contains(missingId.ToString(), error.ErrorMessage);
    }

    // ---Bad input---

    [Fact]
    public async Task GetToDoList_EmptyGuid_Returns400WithInvalidDataError()
    {
        var response = await _client.GetAsync(GetUrl(Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>();
        Assert.Equal("entity.invalid.data", Assert.Single(errors!).ErrorCode);
    }

    /// <summary>
    /// Model binding fails before the action runs; [ApiController] returns ValidationProblemDetails,
    /// so only the status code is asserted.
    /// </summary>
    [Fact]
    public async Task GetToDoList_MalformedId_Returns400()
    {
        var response = await _client.GetAsync(GetUrl("not-a-guid"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ===GetAllToDoLists===

    private const string GetAllUrl = "api/ToDoList/getAllToDoLists";

    private async Task<List<QueryToDoListDto>?> GetAllAsync() =>
        await (await _client.GetAsync(GetAllUrl)).Content.ReadFromJsonAsync<List<QueryToDoListDto>>();

    // ---Lists exist---

    [Fact]
    public async Task GetAllToDoLists_ListsExist_Returns200()
    {
        await SeedListAsync();

        var response = await _client.GetAsync(GetAllUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// No ORDER BY in the query, so compare as sets.
    /// </summary>
    [Fact]
    public async Task GetAllToDoLists_ListsExist_ReturnsEveryList()
    {
        var seeded = new[] { await SeedListAsync(), await SeedListAsync(), await SeedListAsync() };

        var body = await GetAllAsync();

        Assert.NotNull(body);
        Assert.Equal(3, body.Count);
        Assert.True(seeded.Select(l => l.Id).ToHashSet().SetEquals(body.Select(l => l.Id)));
    }

    [Fact]
    public async Task GetAllToDoLists_ListsExist_ReturnsStoredValues()
    {
        var list = await SeedListAsync();
        var saved = await DbContext.ToDoLists.AsNoTracking().SingleAsync(l => l.Id == list.Id);

        var dto = Assert.Single((await GetAllAsync())!);

        Assert.Equal(saved.ListTitle, dto.ListTitle);
        Assert.Equal(saved.ListDescription, dto.ListDescription);
        Assert.Equal(saved.CreatedOn, dto.CreatedOn);
        Assert.Equal(saved.ModifiedOn, dto.ModifiedOn);
    }

    /// <summary>
    /// Documents current behavior: the query has no Include, so items aren't returned.
    /// Accepts null or [] so it doesn't depend on mapper/serializer details.
    /// Flip if the overview starts including items.
    /// </summary>
    [Fact]
    public async Task GetAllToDoLists_ListsWithItems_DoesNotReturnItems()
    {
        await SeedListAsync(itemCount: 3);

        var dto = Assert.Single((await GetAllAsync())!);

        Assert.True(dto.ToDoItems is null || dto.ToDoItems.Count == 0);
    }

    // ---No lists---
    // Pins current behavior (404 on empty). If empty becomes 200 + [], replace with that assertion.

    [Fact]
    public async Task GetAllToDoLists_NoLists_Returns404WithCollectionNotFoundError()
    {
        var response = await _client.GetAsync(GetAllUrl);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>();
        Assert.Equal("entity.collection.not.found", Assert.Single(errors!).ErrorCode);
    }

}
