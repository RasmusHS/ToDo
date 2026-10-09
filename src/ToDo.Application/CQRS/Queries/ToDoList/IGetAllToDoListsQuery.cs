using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Queries;

namespace ToDo.Application.CQRS.Queries.ToDoList;

/// <summary>
/// Interface for the query to get all ToDo lists.
/// Query does not include the ToDo items, only the summary information of each list.
/// </summary>
public interface IGetAllToDoListsQuery
{
    /// <summary>
    /// Gets all ToDo lists, returning a summary of each list.
    /// </summary>
    /// <returns></returns>
    public Task<OneOf<List<QueryToDoListSummaryDto>, List<ErrorResponseDto>>> GetAllAsync();
}
