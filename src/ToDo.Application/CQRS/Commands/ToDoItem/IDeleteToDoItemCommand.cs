using OneOf;
using OneOf.Types;
using ToDo.Application.DTO;

namespace ToDo.Application.CQRS.Commands.ToDoItem;

public interface IDeleteToDoItemCommand
{
    public Task<OneOf<Success, List<ErrorResponseDto>>> DeleteAsync(Guid id);
}
