using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Queries;

namespace ToDo.Application.CQRS.Queries.ToDoList;

public interface IGetAllToDoListsQuery
{
    public Task<OneOf<List<QueryToDoListDto>, List<ErrorResponseDto>>> GetAllAsync();
}
