using ToDo.Webapp.Dto.Queries.ToDoItem;

namespace ToDo.Webapp.Services;

public interface IToDoItemService
{
    Task<List<QueryToDoItemDto>> GetToDoItemsFromListAsync(Guid listId);
    Task<ToDoItemResponseDto> PostToDoItemAsync(CreateToDoItemDto dto);
}
