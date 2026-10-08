public interface IToDoItemService
{
    Task<List<QueryToDoItemDto>> GetToDoItemsFromListAsync(Guid listId);
    Task<ToDoItemResponseDto> PostToDoItemAsync(CreateToDoItemDto dto);
}