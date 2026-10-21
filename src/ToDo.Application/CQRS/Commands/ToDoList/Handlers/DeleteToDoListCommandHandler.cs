using OneOf;
using OneOf.Types;
using ToDo.Application.DTO;
using ToDo.Application.Errors;
using ToDo.Persistence;

namespace ToDo.Application.CQRS.Commands.ToDoList.Handlers;

public class DeleteToDoListCommandHandler : IDeleteToDoListCommand
{
    private readonly ToDoDbContext _dbContext;

    public DeleteToDoListCommandHandler(ToDoDbContext dbContext)
    {
        _dbContext = dbContext;;
    }

    public async Task<OneOf<Success, List<ErrorResponseDto>>> DeleteAsync(Guid id)
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>(); // Initialize an empty list to hold error responses

        var result = await _dbContext.ToDoLists.FindAsync(id);

        if (result == null)
        {
            errors.Add(ToDoListErrors.NotFound(id));
            return errors; // Return the list of errors if the entity is not found
        }

        _dbContext.ToDoLists.Remove(result);
        await _dbContext.SaveChangesAsync();

        return new Success(); // Return the deleted entity mapped to the response DTO
    }
}
