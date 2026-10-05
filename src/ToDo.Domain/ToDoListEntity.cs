namespace ToDo.Domain;

/// <summary>
/// Sealed to prevent inheritance.
/// </summary>
public sealed class ToDoListEntity
{
    /// <summary>
    /// Ignore. internal ctor is needed for EF to create migrations.
    /// </summary>
    internal ToDoListEntity() { } // For EF/ORM

    /// <summary>
    /// ctor for creating a new empty ToDoListEntity
    /// </summary>
    /// <param name="listTitle"></param>
    /// <param name="listDescription"></param>
    public ToDoListEntity(string listTitle, string? listDescription)
    {
        Id = Guid.NewGuid();
        ListTitle = listTitle;
        ListDescription = listDescription;

        DateTime now = DateTime.UtcNow;
        CreatedOn = now;
        ModifiedOn = now;
    }

    // ---Public Methods---

    /// <summary>
    /// Simple method for updating a ToDoListEntity's properties.
    /// </summary>
    /// <param name="listTitle"></param>
    /// <param name="listDescription"></param>
    public void Update(string listTitle, string? listDescription)
    {
        ListTitle = listTitle;
        ListDescription = listDescription;
        ModifiedOn = DateTime.UtcNow;
    }

    // ---Private Methods---


    // ---Properties---

    /// <summary>
    /// Unique PK identifier for each ToDoListEntity.
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// A ToDoListEntity's title, like "Groceries".
    /// </summary>
    public string ListTitle { get; private set; }

    /// <summary>
    /// An optional description for ToDoListEntity.
    /// To add further context/info than what a title can provide.
    /// </summary>
    public string? ListDescription { get; private set; }

    /// <summary>
    /// The date and time on which a ToDoListEntity was created. 
    /// Only set at creation and then never touched again.
    /// </summary>
    public DateTime CreatedOn { get; private init; }

    /// <summary>
    /// The date and time on which a ToDoListEntity was last updated.
    /// Only set in ctor and Update methods.
    /// </summary>
    public DateTime ModifiedOn { get; private set; }

    // ---Navigation Properties---

    /// <summary>
    /// Navigation property to help EF map the relationship and to access associated ToDoItemEntities.
    /// </summary>
    public List<ToDoItemEntity> ToDoItems { get; private set; } = new();
}
