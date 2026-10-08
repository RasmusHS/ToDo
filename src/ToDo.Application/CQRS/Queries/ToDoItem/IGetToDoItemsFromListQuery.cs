using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Queries;

namespace ToDo.Application.CQRS.Queries.ToDoItem;

public interface IGetToDoItemsFromListQuery
{
    public Task<OneOf<List<QueryToDoItemDto>, List<ErrorResponseDto>>> GetAsync(Guid id);
}