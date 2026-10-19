using System.Net;
using System.Net.Http.Json;
using Helpers;
using Microsoft.EntityFrameworkCore;
using ToDo.Application.DTO;
using ToDo.Domain;

namespace ToDo.Api.IntegrationTests;

public class ToDoListControllerDeleteTests : BaseIntegrationTest
{
    private static string DeleteUrl(object id) => $"api/ToDoList/deleteToDoList/{id}";
    private static string GetUrl(object id) => $"api/ToDoList/getToDoList/{id}";

    private readonly HttpClient _client;

    public ToDoListControllerDeleteTests(ToDoWebApplicationFactory factory) : base(factory)
    {
        _client = factory.CreateClient();
    }

    // ---Helpers---

    private IQueryable<ToDoListEntity> Lists => DbContext.ToDoLists.AsNoTracking();
    private IQueryable<ToDoItemEntity> Items => DbContext.ToDoItems.AsNoTracking();

    private async Task<ToDoListEntity> SeedListAsync(int itemCount = 0)
    {
        var list = new ToDoListEntity(
            StringRandom.GetRandomAlphabeticString(20),
            StringRandom.GetRandomAlphabeticString(100));
        DbContext.ToDoLists.Add(list);

        for (var i = 0; i < itemCount; i++)
        {
            DbContext.ToDoItems.Add(new ToDoItemEntity(list.Id, StringRandom.GetRandomAlphabeticString(50), null));
        }

        await DbContext.SaveChangesAsync();
        return list;
    }

    // ---Happy path---

    [Fact]
    public async Task DeleteToDoList_ExistingId_Returns204WithEmptyBody()
    {
        var list = await SeedListAsync();

        var response = await _client.DeleteAsync(DeleteUrl(list.Id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteToDoList_ExistingId_RemovesList()
    {
        var list = await SeedListAsync();

        await _client.DeleteAsync(DeleteUrl(list.Id));

        Assert.False(await Lists.AnyAsync(l => l.Id == list.Id));
    }

    /// <summary>
    /// The handler doesn't load items, so this depends entirely on ON DELETE CASCADE in Postgres.
    /// Fails with a 500 (FK violation) if the relationship is ever configured as Restrict.
    /// </summary>
    [Fact]
    public async Task DeleteToDoList_ListWithItems_DeletesItsItems()
    {
        var list = await SeedListAsync(itemCount: 3);

        var response = await _client.DeleteAsync(DeleteUrl(list.Id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await Items.AnyAsync(i => i.ToDoListId == list.Id));
    }

    [Fact]
    public async Task DeleteToDoList_ExistingId_DoesNotAffectOtherListsOrTheirItems()
    {
        var target = await SeedListAsync(itemCount: 2);
        var other = await SeedListAsync(itemCount: 3);

        await _client.DeleteAsync(DeleteUrl(target.Id));

        Assert.Equal(other.Id, (await Lists.SingleAsync()).Id);
        Assert.Equal(3, await Items.CountAsync());
        Assert.True(await Items.AllAsync(i => i.ToDoListId == other.Id));
    }

    [Fact]
    public async Task DeleteToDoList_ThenGet_Returns404()
    {
        var list = await SeedListAsync(itemCount: 1);

        await _client.DeleteAsync(DeleteUrl(list.Id));
        var response = await _client.GetAsync(GetUrl(list.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteToDoList_SameIdTwice_SecondReturns404()
    {
        var list = await SeedListAsync();

        var first = await _client.DeleteAsync(DeleteUrl(list.Id));
        var second = await _client.DeleteAsync(DeleteUrl(list.Id));

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    // ---Not found---

    [Fact]
    public async Task DeleteToDoList_NonExistentId_Returns404WithNotFoundError()
    {
        await SeedListAsync();
        var missingId = Guid.NewGuid();

        var response = await _client.DeleteAsync(DeleteUrl(missingId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>();
        var error = Assert.Single(errors!);
        Assert.Equal("entity.not.found", error.ErrorCode);
        Assert.Contains(missingId.ToString(), error.ErrorMessage);
    }

    [Fact]
    public async Task DeleteToDoList_NonExistentId_DoesNotRemoveAnything()
    {
        var list = await SeedListAsync(itemCount: 2);

        await _client.DeleteAsync(DeleteUrl(Guid.NewGuid()));

        Assert.Equal(list.Id, (await Lists.SingleAsync()).Id);
        Assert.Equal(2, await Items.CountAsync());
    }

    // ---Bad input---

    /// <summary>
    /// Pins current behavior: no Guid.Empty guard here (unlike GET, which returns 400),
    /// so it falls through to the handler's not-found.
    /// </summary>
    [Fact]
    public async Task DeleteToDoList_EmptyGuid_Returns404()
    {
        var response = await _client.DeleteAsync(DeleteUrl(Guid.Empty));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Model binding fails before the action runs; [ApiController] returns ValidationProblemDetails.
    /// </summary>
    [Fact]
    public async Task DeleteToDoList_MalformedId_Returns400()
    {
        var response = await _client.DeleteAsync(DeleteUrl("not-a-guid"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}
