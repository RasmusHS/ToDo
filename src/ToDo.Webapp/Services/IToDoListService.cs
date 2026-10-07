
public interface IToDoListService
{
    Task<ToDoListResponseDto> PostToDoListAsync(CreateToDoListDto dto);

    Task<QueryToDoListDto> GetToDoListAsync(Guid id);

    Task<List<QueryToDoListDto>> GetAllToDoListsAsync();
    
}