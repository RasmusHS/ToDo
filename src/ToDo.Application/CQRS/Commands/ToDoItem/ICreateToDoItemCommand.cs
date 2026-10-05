using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoItem;

namespace ToDo.Application.CQRS.Commands.ToDoItem;

public interface ICreateToDoItemCommand
{
    public Task<OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>> CreateAsync(CreateToDoItemDto dto);
}
