using Helpers;

namespace ToDo.Domain.Tests;

public class ToDoItemEntityTests
{
    // Arbitrary until the group settles on max lengths; swap for the real constants then.
    private const int TextLength = 50;
    private const int StatusLength = 15;

    private static string RandomText() => StringRandom.GetRandomString(TextLength);
    private static string RandomStatus() => StringRandom.GetRandomString(StatusLength);

    private static ToDoItemEntity CreateItem(Guid? listId = null) =>
        new(listId ?? Guid.NewGuid(), RandomText(), RandomStatus());

    // ---Constructor---

    [Fact]
    public void Ctor_SetsListIdTextAndStatus()
    {
        var listId = Guid.NewGuid();
        var text = RandomText();
        var status = RandomStatus();

        var item = new ToDoItemEntity(listId, text, status);

        Assert.Equal(listId, item.ToDoListId);
        Assert.Equal(text, item.Text);
        Assert.Equal(status, item.Status);
    }

    [Fact]
    public void Ctor_AllowsNullStatus()
    {
        var item = new ToDoItemEntity(Guid.NewGuid(), RandomText(), null);

        Assert.Null(item.Status);
    }

    [Fact]
    public void Ctor_IsDoneDefaultsToFalse()
    {
        var item = CreateItem();

        Assert.False(item.IsDone);
    }

    [Fact]
    public void Ctor_GeneratesNonEmptyId()
    {
        var item = CreateItem();

        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public void Ctor_GeneratesUniqueIdPerInstance()
    {
        var a = CreateItem();
        var b = CreateItem();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Ctor_IdDiffersFromListId()
    {
        var listId = Guid.NewGuid();

        var item = CreateItem(listId);

        Assert.NotEqual(listId, item.Id);
    }

    [Fact]
    public void Ctor_SetsCreatedOnToCurrentUtcTime()
    {
        var before = DateTime.UtcNow;
        var item = CreateItem();
        var after = DateTime.UtcNow;

        Assert.InRange(item.CreatedOn, before, after);
        Assert.Equal(DateTimeKind.Utc, item.CreatedOn.Kind);
    }

    [Fact]
    public void Ctor_ModifiedOnEqualsCreatedOn()
    {
        var item = CreateItem();

        Assert.Equal(item.CreatedOn, item.ModifiedOn);
    }

    // ---Update---
    // Red until Update is implemented.

    [Fact]
    public void Update_ChangesTextAndStatus()
    {
        var item = CreateItem();
        var newText = RandomText();
        var newStatus = RandomStatus();

        item.Update(item.IsDone, newText, newStatus);

        Assert.Equal(newText, item.Text);
        Assert.Equal(newStatus, item.Status);
    }

    [Fact]
    public void Update_CanClearStatus()
    {
        var item = CreateItem();

        item.Update(item.IsDone, item.Text, null);

        Assert.Null(item.Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Update_SetsIsDone(bool isDone)
    {
        var item = CreateItem();

        item.Update(isDone, item.Text, item.Status);

        Assert.Equal(isDone, item.IsDone);
    }

    [Fact]
    public void Update_CanUndoIsDone()
    {
        var item = CreateItem();
        item.Update(true, item.Text, item.Status);

        item.Update(false, item.Text, item.Status);

        Assert.False(item.IsDone);
    }

    [Fact]
    public void Update_RefreshesModifiedOn()
    {
        var item = CreateItem();

        var before = DateTime.UtcNow;
        item.Update(true, RandomText(), RandomStatus());
        var after = DateTime.UtcNow;

        Assert.InRange(item.ModifiedOn, before, after);
        Assert.True(item.ModifiedOn >= item.CreatedOn);
        Assert.Equal(DateTimeKind.Utc, item.ModifiedOn.Kind);
    }

    [Fact]
    public void Update_DoesNotChangeIdListIdOrCreatedOn()
    {
        var item = CreateItem();
        var id = item.Id;
        var listId = item.ToDoListId;
        var createdOn = item.CreatedOn;

        item.Update(true, RandomText(), RandomStatus());

        Assert.Equal(id, item.Id);
        Assert.Equal(listId, item.ToDoListId);
        Assert.Equal(createdOn, item.CreatedOn);
    }
}
