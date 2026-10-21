using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using Helpers;
using Microsoft.EntityFrameworkCore;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Queries;
using ToDo.Domain;

namespace ToDo.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public class ToDoItemControllerDeleteTests : BaseIntegrationTest
{
    private static string DeleteUrl(object id) => $"api/ToDoItem/deleteToDoItem/{id}";
    private static string GetListUrl(object id) => $"api/ToDoList/getToDoList/{id}";

    private readonly HttpClient _client;

    public ToDoItemControllerDeleteTests(ToDoWebApplicationFactory factory) : base(factory)
    {
        _client = factory.CreateClient();
    }

    // ---Helpers---

    private IQueryable<ToDoListEntity> Lists => DbContext.ToDoLists.AsNoTracking();
    private IQueryable<ToDoItemEntity> Items => DbContext.ToDoItems.AsNoTracking();

    /// <summary>
    /// Seeds one list with items and returns the items.
    /// </summary>
    private async Task<List<ToDoItemEntity>> SeedItemsAsync(int count = 1)
    {
        var list = new ToDoListEntity(StringRandom.GetRandomAlphabeticString(20), null);
        DbContext.ToDoLists.Add(list);

        var items = Enumerable.Range(0, count)
            .Select(_ => new ToDoItemEntity(list.Id, StringRandom.GetRandomAlphabeticString(50), null))
            .ToList();
        DbContext.ToDoItems.AddRange(items);

        await DbContext.SaveChangesAsync();
        return items;
    }

    private async Task<ToDoItemEntity> SeedItemAsync() => (await SeedItemsAsync()).Single();

    // ---Happy path---

    [Fact]
    public async Task DeleteToDoItem_ExistingId_Returns204WithEmptyBody()
    {
        var item = await SeedItemAsync();

        var response = await _client.DeleteAsync(DeleteUrl(item.Id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteToDoItem_ExistingId_RemovesItem()
    {
        var item = await SeedItemAsync();

        await _client.DeleteAsync(DeleteUrl(item.Id));

        Assert.False(await Items.AnyAsync(i => i.Id == item.Id));
    }

    [Fact]
    public async Task DeleteToDoItem_ExistingId_KeepsParentListAndSiblings()
    {
        var items = await SeedItemsAsync(count: 3);

        await _client.DeleteAsync(DeleteUrl(items[0].Id));

        Assert.True(await Lists.AnyAsync(l => l.Id == items[0].ToDoListId));
        var remainingIds = await Items.Select(i => i.Id).ToListAsync();
        Assert.Equal(items.Skip(1).Select(i => i.Id).Order(), remainingIds.Order());
    }

    [Fact]
    public async Task DeleteToDoItem_ItemInOtherList_DoesNotAffectOtherLists()
    {
        var target = await SeedItemAsync();
        var otherListItems = await SeedItemsAsync(count: 2);

        await _client.DeleteAsync(DeleteUrl(target.Id));

        Assert.Equal(2, await Items.CountAsync(i => i.ToDoListId == otherListItems[0].ToDoListId));
    }

    /// <summary>
    /// End to end: the list read back through the API no longer contains the deleted item.
    /// </summary>
    [Fact]
    public async Task DeleteToDoItem_ThenGetList_ListNoLongerContainsItem()
    {
        var items = await SeedItemsAsync(count: 2);

        await _client.DeleteAsync(DeleteUrl(items[0].Id));
        var body = await (await _client.GetAsync(GetListUrl(items[0].ToDoListId)))
            .Content.ReadFromJsonAsync<QueryToDoListDto>();

        Assert.NotNull(body?.ToDoItems);
        var remaining = Assert.Single(body.ToDoItems);
        Assert.Equal(items[1].Id, remaining.Id);
    }

    [Fact]
    public async Task DeleteToDoItem_LastItem_ListRemainsWithNoItems()
    {
        var item = await SeedItemAsync();

        await _client.DeleteAsync(DeleteUrl(item.Id));
        var response = await _client.GetAsync(GetListUrl(item.ToDoListId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<QueryToDoListDto>();
        Assert.NotNull(body?.ToDoItems);
        Assert.Empty(body.ToDoItems);
    }

    [Fact]
    public async Task DeleteToDoItem_SameIdTwice_SecondReturns404()
    {
        var item = await SeedItemAsync();

        var first = await _client.DeleteAsync(DeleteUrl(item.Id));
        var second = await _client.DeleteAsync(DeleteUrl(item.Id));

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    // ---Not found---

    [Fact]
    public async Task DeleteToDoItem_NonExistentId_Returns404WithNotFoundError()
    {
        await SeedItemAsync();
        var missingId = Guid.NewGuid();

        var response = await _client.DeleteAsync(DeleteUrl(missingId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>();
        var error = Assert.Single(errors!);
        Assert.Equal("entity.not.found", error.ErrorCode);
        Assert.Contains(missingId.ToString(), error.ErrorMessage);
    }

    [Fact]
    public async Task DeleteToDoItem_NonExistentId_DoesNotRemoveAnything()
    {
        await SeedItemsAsync(count: 2);

        await _client.DeleteAsync(DeleteUrl(Guid.NewGuid()));

        Assert.Equal(2, await Items.CountAsync());
        Assert.Equal(1, await Lists.CountAsync());
    }

    /// <summary>
    /// A list ID sent to the item endpoint must not delete the list (and cascade its items).
    /// </summary>
    [Fact]
    public async Task DeleteToDoItem_ListIdInsteadOfItemId_Returns404AndDeletesNothing()
    {
        var item = await SeedItemAsync();

        var response = await _client.DeleteAsync(DeleteUrl(item.ToDoListId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(await Lists.AnyAsync(l => l.Id == item.ToDoListId));
        Assert.True(await Items.AnyAsync(i => i.Id == item.Id));
    }

    // ---Bad input---

    [Fact]
    public async Task DeleteToDoItem_EmptyGuid_Returns404()
    {
        var response = await _client.DeleteAsync(DeleteUrl(Guid.Empty));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Model binding fails before the action runs; [ApiController] returns ValidationProblemDetails.
    /// </summary>
    [Fact]
    public async Task DeleteToDoItem_MalformedId_Returns400()
    {
        var response = await _client.DeleteAsync(DeleteUrl("not-a-guid"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}
