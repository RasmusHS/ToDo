namespace ToDo.Domain;

/// <summary>
/// Sealed to prevent inheritance.
/// </summary>
public sealed class ToDoItemEntity
{
    /// <summary>
    /// Ignore. internal ctor is needed for EF to create migrations.
    /// </summary>
    internal ToDoItemEntity() { } // For EF/ORM

    /// <summary>
    /// ctor for creating a new ToDoItemEntity in a ToDoListEntity.
    /// </summary>
    /// <param name="toDoListId"></param>
    /// <param name="text"></param>
    /// <param name="status"></param>
    public ToDoItemEntity(Guid toDoListId, string text, string? status)
    {
        Id = Guid.NewGuid();
        ToDoListId = toDoListId;
        IsDone = false; // default value
        Text = text;
        Status = status;

        DateTime now = DateTime.UtcNow;
        CreatedOn = now;
        ModifiedOn = now;
    }

    // ---Public Methods---

    /// <summary>
    /// Simple method for updating a ToDoItemEntity's properties.
    /// </summary>
    /// <param name="isDone"></param>
    /// <param name="text"></param>
    /// <param name="status"></param>
    public void Update(bool isDone, string text, string? status)
    {
        IsDone = isDone;
        Text = text;
        Status = status;
        ModifiedOn = DateTime.UtcNow;
    }

    // ---Private Methods---


    // ---Properties---

    /// <summary>
    /// Unique PK identifier for each ToDoItemEntity.
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// FK identifier for which ToDoListEntity that this ToDoItemEntity belongs to.
    /// </summary>
    public Guid ToDoListId { get; private init; }

    /// <summary>
    /// Indicates whether this ToDoItemEntity is done.
    /// Default is set to false in the ctor
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    /// The text of a ToDoItemEntity that tells what this item is about, like "Touch grass".
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// A status for a given ToDoItemEntity.
    /// Set as nullable as not all items have any need for a status, like simple groceries where "IsDone" is enough.
    /// Shortterm the values will be taken from a list here in the domain, but a better solution would be a json file.  
    /// </summary>
    public string? Status { get; private set; }

    /// <summary>
    /// The date and time on which a ToDoItemEntity was created. 
    /// Only set at creation and then never touched again.
    /// </summary>
    public DateTime CreatedOn { get; private init; }

    /// <summary>
    /// The date and time on which a ToDoItemEntity was last updated.
    /// Only set in ctor and Update methods.
    /// </summary>
    public DateTime ModifiedOn { get; private set; }

    // ---Navigation Properties---

    /// <summary>
    /// Navigation property to help EF map the relationship and to access associated ToDoListEntity properties.
    /// </summary>
    public ToDoListEntity ToDoList { get; private set; }
}
