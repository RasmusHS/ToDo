using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Queries;

namespace ToDo.Application.CQRS.Queries.ToDoList;

/// <summary>
/// Interface for the query to get a specific ToDo list by its ID.
/// Query includes the ToDo items associated with the list.
/// </summary>
public interface IGetToDoListQuery
{
    /// <summary>
    /// Gets a specific ToDo list by its ID, including the ToDo items associated with the list.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<OneOf<QueryToDoListDto, List<ErrorResponseDto>>> GetAsync(Guid id);
}
