using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoList;

namespace ToDo.Application.CQRS.Commands.ToDoList;

public interface ICreateToDoListCommand
{
    // Create method that returns a Task of type OneOf<ToDoListResponseDto, List<ErrorResponseDto>> and takes a parameter of type CreateToDoListDto
    public Task<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>> CreateAsync(CreateToDoListDto dto);
}
