using OneOf;
using OneOf.Types;
using ToDo.Application.DTO;
using ToDo.Application.Errors;
using ToDo.Persistence;

namespace ToDo.Application.CQRS.Commands.ToDoItem.Handlers;

public class DeleteToDoItemCommandHandler : IDeleteToDoItemCommand
{
    private readonly ToDoDbContext _dbContext;

    public DeleteToDoItemCommandHandler(ToDoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OneOf<Success, List<ErrorResponseDto>>> DeleteAsync(Guid id)
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>(); // Initialize an empty list to hold error responses

        var toDoItem = await _dbContext.ToDoItems.FindAsync(id);

        if (toDoItem == null)
        {
            errors.Add(ToDoItemErrors.NotFound(id));
            return errors;
        }

        _dbContext.ToDoItems.Remove(toDoItem);
        await _dbContext.SaveChangesAsync();

        return new Success();
    }
}
