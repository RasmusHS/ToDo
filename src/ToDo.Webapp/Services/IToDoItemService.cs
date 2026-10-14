
namespace ToDo.Webapp.Services;

public interface IToDoItemService
{
    Task<ToDoItemResponseDto> PostToDoItemAsync(CreateToDoItemDto dto);
}
