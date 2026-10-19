using OneOf;
using OneOf.Types;
using ToDo.Application.DTO;

namespace ToDo.Application.CQRS.Commands.ToDoList;

public interface IDeleteToDoListCommand
{
    public Task<OneOf<Success, List<ErrorResponseDto>>> DeleteAsync(Guid id);
}
