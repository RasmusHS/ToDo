public class UpdateToDoListDto
{
    /// <summary>
    /// Unique PK identifier for each ToDoListEntity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// A ToDoListEntity's title, like "Groceries".
    /// </summary>
    public string ListTitle { get; set; }

    /// <summary>
    /// An optional description for ToDoListEntity.
    /// To add further context/info than what a title can provide.
    /// </summary>
    public string? ListDescription { get; set; }
}
