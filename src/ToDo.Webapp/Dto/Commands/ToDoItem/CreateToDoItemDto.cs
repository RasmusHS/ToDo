public class CreateToDoItemDto
{
    
    // <summary>
    /// FK identifier for which ToDoListEntity that this ToDoItemEntity belongs to.
    /// </summary>
    public Guid ToDoListId { get; set; }

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