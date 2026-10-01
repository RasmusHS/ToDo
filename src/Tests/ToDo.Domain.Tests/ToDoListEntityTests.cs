using Helpers;

namespace ToDo.Domain.Tests;

public class ToDoListEntityTests
{
    // Arbitrary until the group settles on max lengths; swap for the real constants then.
    private const int TitleLength = 20;
    private const int DescriptionLength = 100;

    private static string RandomTitle() => StringRandom.GetRandomString(TitleLength);
    private static string RandomDescription() => StringRandom.GetRandomString(DescriptionLength);

    private static ToDoListEntity CreateList() => new(RandomTitle(), RandomDescription());

    // ---Constructor---

    [Fact]
    public void Ctor_SetsTitleAndDescription()
    {
        var title = RandomTitle();
        var description = RandomDescription();

        var list = new ToDoListEntity(title, description);

        Assert.Equal(title, list.ListTitle);
        Assert.Equal(description, list.ListDescription);
    }

    [Fact]
    public void Ctor_AllowsNullDescription()
    {
        var list = new ToDoListEntity(RandomTitle(), null);

        Assert.Null(list.ListDescription);
    }

    [Fact]
    public void Ctor_GeneratesNonEmptyId()
    {
        var list = CreateList();

        Assert.NotEqual(Guid.Empty, list.Id);
    }

    [Fact]
    public void Ctor_GeneratesUniqueIdPerInstance()
    {
        var a = CreateList();
        var b = CreateList();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Ctor_SetsCreatedOnToCurrentUtcTime()
    {
        var before = DateTime.UtcNow;
        var list = CreateList();
        var after = DateTime.UtcNow;

        Assert.InRange(list.CreatedOn, before, after);
        Assert.Equal(DateTimeKind.Utc, list.CreatedOn.Kind);
    }

    [Fact]
    public void Ctor_ModifiedOnEqualsCreatedOn()
    {
        var list = CreateList();

        Assert.Equal(list.CreatedOn, list.ModifiedOn);
    }

    [Fact]
    public void Ctor_InitializesEmptyToDoItems()
    {
        var list = CreateList();

        Assert.NotNull(list.ToDoItems);
        Assert.Empty(list.ToDoItems);
    }

    // ---Update---
    // Red until Update is implemented.

    [Fact]
    public void Update_ChangesTitleAndDescription()
    {
        var list = CreateList();
        var newTitle = RandomTitle();
        var newDescription = RandomDescription();

        list.Update(newTitle, newDescription);

        Assert.Equal(newTitle, list.ListTitle);
        Assert.Equal(newDescription, list.ListDescription);
    }

    [Fact]
    public void Update_CanClearDescription()
    {
        var list = CreateList();

        list.Update(list.ListTitle, null);

        Assert.Null(list.ListDescription);
    }

    [Fact]
    public void Update_RefreshesModifiedOn()
    {
        var list = CreateList();

        var before = DateTime.UtcNow;
        list.Update(RandomTitle(), RandomDescription());
        var after = DateTime.UtcNow;

        Assert.InRange(list.ModifiedOn, before, after);
        Assert.True(list.ModifiedOn >= list.CreatedOn);
        Assert.Equal(DateTimeKind.Utc, list.ModifiedOn.Kind);
    }

    [Fact]
    public void Update_DoesNotChangeIdOrCreatedOn()
    {
        var list = CreateList();
        var id = list.Id;
        var createdOn = list.CreatedOn;

        list.Update(RandomTitle(), RandomDescription());

        Assert.Equal(id, list.Id);
        Assert.Equal(createdOn, list.CreatedOn);
    }

    [Fact]
    public void Update_DoesNotTouchToDoItems()
    {
        var list = CreateList();
        var items = list.ToDoItems;

        list.Update(RandomTitle(), RandomDescription());

        Assert.Same(items, list.ToDoItems);
    }
}
