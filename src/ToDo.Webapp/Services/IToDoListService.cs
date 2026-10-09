using ToDo.Webapp.Dto.Queries.ToDoList;

namespace ToDo.Webapp.Services;

public interface IToDoListService
{
    /// <summary>
    /// Posts a new ToDoListEntity to the API and returns the created entity's details.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    Task<ToDoListResponseDto> PostToDoListAsync(CreateToDoListDto dto);

    /// <summary>
    /// Gets a ToDoListEntity by its unique identifier (id) from the API and returns its details including associated ToDoItemEntities.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<QueryToDoListDto> GetToDoListAsync(Guid id);

    /// <summary>
    /// Gets all ToDoListEntities from the API without their associated ToDoItemEntities.
    /// </summary>
    /// <returns></returns>
    Task<List<QueryToDoListSummaryDto>> GetAllToDoListsAsync();
    
}
