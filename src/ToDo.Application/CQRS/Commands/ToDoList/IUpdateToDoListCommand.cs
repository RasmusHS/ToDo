using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoList;

namespace ToDo.Application.CQRS.Commands.ToDoList;

public interface IUpdateToDoListCommand
{
    public Task<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>> UpdateAsync(UpdateToDoListDto dto);
}