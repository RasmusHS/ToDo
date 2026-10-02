using ToDo.Domain;

namespace ToDo.Application.DTO.Commands.ToDoList;

public class ToDoListResponseDto
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

    /// <summary>
    /// The date and time on which a ToDoListEntity was created. 
    /// Only set at creation and then never touched again.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// The date and time on which a ToDoListEntity was last updated.
    /// Only set in ctor and Update methods.
    /// </summary>
    public DateTime ModifiedOn { get; set; }

    /// <summary>
    /// Navigation property to help EF map the relationship and to access associated ToDoItemEntities.
    /// </summary>
    public List<ToDoItemEntity> ToDoItems { get; set; }
}
