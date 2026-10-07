using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Queries;

namespace ToDo.Application.CQRS.Queries.ToDoList;

public interface IGetToDoListQuery
{
    public Task<OneOf<QueryToDoListDto, List<ErrorResponseDto>>> GetAsync(Guid id);
}
