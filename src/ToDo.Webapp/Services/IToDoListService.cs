
public interface IToDoListService
{
    Task<ToDoListResponseDto> PostToDoListAsync(CreateToDoListDto dto);
    
}