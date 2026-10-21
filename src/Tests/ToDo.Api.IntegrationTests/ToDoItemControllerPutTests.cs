using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using Helpers;
using Microsoft.EntityFrameworkCore;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoItem;
using ToDo.Domain;

namespace ToDo.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public class ToDoItemControllerPutTests : BaseIntegrationTest
{
    private const string PutUrl = "api/ToDoItem/putToDoItem";

    // Match UpdateToDoItemDto.Validator.
    private const int TextMaxLength = 500;
    private const int StatusMaxLength = 100;

    private const int TextLength = 50;
    private const int StatusLength = 15;

    private readonly HttpClient _client;

    public ToDoItemControllerPutTests(ToDoWebApplicationFactory factory) : base(factory)
    {
        _client = factory.CreateClient();
    }

    // ---Helpers---

    private IQueryable<ToDoItemEntity> Items => DbContext.ToDoItems.AsNoTracking();

    /// <summary>
    /// Seeds one list with items and returns them as stored, so timestamps carry Postgres' microsecond precision.
    /// </summary>
    private async Task<List<ToDoItemEntity>> SeedItemsAsync(int count = 1)
    {
        var list = new ToDoListEntity(StringRandom.GetRandomAlphabeticString(20), null);
        DbContext.ToDoLists.Add(list);

        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var item = new ToDoItemEntity(
                list.Id,
                StringRandom.GetRandomAlphabeticString(TextLength),
                StringRandom.GetRandomAlphabeticString(StatusLength));
            DbContext.ToDoItems.Add(item);
            ids.Add(item.Id);
        }

        await DbContext.SaveChangesAsync();
        return await Items.Where(i => ids.Contains(i.Id)).ToListAsync();
    }

    private async Task<ToDoItemEntity> SeedItemAsync() => (await SeedItemsAsync()).Single();

    private static UpdateToDoItemDto UpdateDto(
        Guid id, bool isDone = true, string? text = null, bool withStatus = true, string? status = null) => new()
        {
            Id = id,
            IsDone = isDone,
            Text = text ?? StringRandom.GetRandomAlphabeticString(TextLength),
            Status = withStatus ? status ?? StringRandom.GetRandomAlphabeticString(StatusLength) : null
        };

    private static UpdateToDoItemDto UpdateDto(ToDoItemEntity item, bool isDone = true) =>
        UpdateDto(item.Id, isDone);

    private async Task<List<ErrorResponseDto>> ReadErrorsAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<List<ErrorResponseDto>>())!;

    // ---Happy path---

    [Fact]
    public async Task PutToDoItem_ValidDto_Returns200WithUpdatedItem()
    {
        var item = await SeedItemAsync();
        var dto = UpdateDto(item);

        var response = await _client.PutAsJsonAsync(PutUrl, dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ToDoItemResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(item.Id, body.Id);
        Assert.Equal(item.ToDoListId, body.ToDoListId);
        Assert.Equal(dto.Text, body.Text);
        Assert.Equal(dto.Status, body.Status);
        Assert.True(body.IsDone);
    }

    [Fact]
    public async Task PutToDoItem_ValidDto_PersistsChanges()
    {
        var item = await SeedItemAsync();
        var dto = UpdateDto(item);

        await _client.PutAsJsonAsync(PutUrl, dto);

        var saved = await Items.SingleAsync();
        Assert.Equal(dto.Text, saved.Text);
        Assert.Equal(dto.Status, saved.Status);
        Assert.True(saved.IsDone);
        Assert.Equal(item.ToDoListId, saved.ToDoListId);
    }

    [Fact]
    public async Task PutToDoItem_ValidDto_RefreshesModifiedOnAndKeepsCreatedOn()
    {
        var item = await SeedItemAsync();

        var before = DateTime.UtcNow;
        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(item));
        var after = DateTime.UtcNow;

        var body = await response.Content.ReadFromJsonAsync<ToDoItemResponseDto>();
        var saved = await Items.SingleAsync();

        Assert.Equal(item.CreatedOn, saved.CreatedOn);
        Assert.Equal(item.CreatedOn, body!.CreatedOn);
        Assert.InRange(body.ModifiedOn, before, after);
        Assert.True(saved.ModifiedOn > item.ModifiedOn);
        // Body from in-memory entity (100 ns ticks), row stored with microseconds.
        Assert.Equal(body.ModifiedOn, saved.ModifiedOn, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task PutToDoItem_DoneThenUndone_PersistsUndone()
    {
        var item = await SeedItemAsync();

        await _client.PutAsJsonAsync(PutUrl, UpdateDto(item, isDone: true));
        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(item, isDone: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await Items.SingleAsync()).IsDone);
    }

    [Fact]
    public async Task PutToDoItem_NullStatus_ClearsStatus()
    {
        var item = await SeedItemAsync();

        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(item.Id, withStatus: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null((await Items.SingleAsync()).Status);
    }

    [Fact]
    public async Task PutToDoItem_ValidDto_DoesNotAffectSiblingItems()
    {
        var items = await SeedItemsAsync(count: 3);

        await _client.PutAsJsonAsync(PutUrl, UpdateDto(items[0]));

        foreach (var sibling in items.Skip(1))
        {
            var untouched = await Items.SingleAsync(i => i.Id == sibling.Id);
            Assert.Equal(sibling.Text, untouched.Text);
            Assert.Equal(sibling.Status, untouched.Status);
            Assert.Equal(sibling.IsDone, untouched.IsDone);
            Assert.Equal(sibling.ModifiedOn, untouched.ModifiedOn);
        }
    }

    /// <summary>
    /// Also checks the database columns are wide enough for what the validator allows.
    /// </summary>
    [Fact]
    public async Task PutToDoItem_TextAndStatusAtMaxLength_Returns200AndPersists()
    {
        var item = await SeedItemAsync();
        var dto = UpdateDto(item.Id,
            text: StringRandom.GetRandomAlphabeticString(TextMaxLength),
            status: StringRandom.GetRandomAlphabeticString(StatusMaxLength));

        var response = await _client.PutAsJsonAsync(PutUrl, dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await Items.SingleAsync();
        Assert.Equal(dto.Text, saved.Text);
        Assert.Equal(dto.Status, saved.Status);
    }

    // ---Not found---
    // Controller maps every handler error to 400. If not-found becomes 404, update the status here.

    [Fact]
    public async Task PutToDoItem_NonExistentId_Returns400WithNotFoundError()
    {
        var item = await SeedItemAsync();
        var missingId = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(missingId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single(await ReadErrorsAsync(response));
        Assert.Equal("entity.not.found", error.ErrorCode);
        Assert.Contains(missingId.ToString(), error.ErrorMessage);
    }

    [Fact]
    public async Task PutToDoItem_NonExistentId_DoesNotCreateItem()
    {
        var item = await SeedItemAsync();

        await _client.PutAsJsonAsync(PutUrl, UpdateDto(Guid.NewGuid()));

        Assert.Equal(item.Id, (await Items.SingleAsync()).Id);
    }

    // ---Validation---
    // Error codes are FluentValidation's defaults (the validator's name), since the rules don't set WithErrorCode.

    [Fact]
    public async Task PutToDoItem_EmptyId_Returns400WithNotEmptyError()
    {
        var item = await SeedItemAsync();

        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single(await ReadErrorsAsync(response));
        Assert.Equal("NotEmptyValidator", error.ErrorCode);
        Assert.Equal("Id is required.", error.ErrorMessage);
    }

    [Fact]
    public async Task PutToDoItem_TextOverMaxLength_Returns400WithMaxLengthError()
    {
        var item = await SeedItemAsync();
        var dto = UpdateDto(item.Id, text: StringRandom.GetRandomAlphabeticString(TextMaxLength + 1));

        var response = await _client.PutAsJsonAsync(PutUrl, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single(await ReadErrorsAsync(response));
        Assert.Equal("MaximumLengthValidator", error.ErrorCode);
        Assert.Equal("Text cannot exceed 500 characters.", error.ErrorMessage);
    }

    [Fact]
    public async Task PutToDoItem_StatusOverMaxLength_Returns400WithMaxLengthError()
    {
        var item = await SeedItemAsync();
        var dto = UpdateDto(item.Id, status: StringRandom.GetRandomAlphabeticString(StatusMaxLength + 1));

        var response = await _client.PutAsJsonAsync(PutUrl, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single(await ReadErrorsAsync(response));
        Assert.Equal("MaximumLengthValidator", error.ErrorCode);
        Assert.Equal("Status cannot exceed 100 characters.", error.ErrorMessage);
    }

    [Fact]
    public async Task PutToDoItem_MultipleRuleViolations_ReturnsAllErrors()
    {
        var dto = UpdateDto(Guid.Empty,
            text: StringRandom.GetRandomAlphabeticString(TextMaxLength + 1),
            status: StringRandom.GetRandomAlphabeticString(StatusMaxLength + 1));

        var response = await _client.PutAsJsonAsync(PutUrl, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(3, (await ReadErrorsAsync(response)).Count);
    }

    /// <summary>
    /// Status only: empty Text may be rejected by [ApiController]'s implicit [Required]
    /// (ValidationProblemDetails) before FluentValidation runs.
    /// </summary>
    [Fact]
    public async Task PutToDoItem_EmptyText_Returns400()
    {
        var item = await SeedItemAsync();

        var response = await _client.PutAsJsonAsync(PutUrl, UpdateDto(item.Id, text: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutToDoItem_InvalidDto_DoesNotPersist()
    {
        var item = await SeedItemAsync();

        await _client.PutAsJsonAsync(PutUrl,
            UpdateDto(item.Id, text: StringRandom.GetRandomAlphabeticString(TextMaxLength + 1)));

        var unchanged = await Items.SingleAsync();
        Assert.Equal(item.Text, unchanged.Text);
        Assert.Equal(item.IsDone, unchanged.IsDone);
        Assert.Equal(item.ModifiedOn, unchanged.ModifiedOn);
    }

}
