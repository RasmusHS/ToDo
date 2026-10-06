using FluentValidation;

namespace ToDo.Application.DTO.Commands.ToDoList;

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

    public class Validator : AbstractValidator<UpdateToDoListDto>
    {
        public Validator()
        {
            RuleFor(x => x.ListTitle)
                .NotEmpty().WithMessage("List title is required.")
                .MaximumLength(200).WithMessage("List title cannot exceed 200 characters.");
            RuleFor(x => x.ListDescription)
                .MaximumLength(500).WithMessage("List description cannot exceed 500 characters.");
        }
    }
}
