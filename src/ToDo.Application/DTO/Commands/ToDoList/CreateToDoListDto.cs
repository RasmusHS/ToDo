using FluentValidation;

namespace ToDo.Application.DTO.Commands.ToDoList;

public class CreateToDoListDto
{
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
    /// Navigation property to help EF map the relationship and to access associated ToDoItemEntities.
    /// </summary>
    //public List<CreateToDoItemDto>? ToDoItems { get; set; }

    public class Validator : AbstractValidator<CreateToDoListDto>
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
