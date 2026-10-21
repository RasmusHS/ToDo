public class UpdateToDoItemDto
{
    /// <summary>
    /// Unique PK identifier for each ToDoItemEntity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Indicates whether this ToDoItemEntity is done.
    /// Default is set to false in the ctor
    /// </summary>
    public bool IsDone { get; set; }

    /// <summary>
    /// The text of a ToDoItemEntity that tells what this item is about, like "Touch grass".
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// A status for a given ToDoItemEntity.
    /// Set as nullable as not all items have any need for a status, like simple groceries where "IsDone" is enough.
    /// Shortterm the values will be taken from a list here in the domain, but a better solution would be a json file.  
    /// </summary>
    public string? Status { get; set; }
}
