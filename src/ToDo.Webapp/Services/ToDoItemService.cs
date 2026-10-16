
namespace ToDo.Webapp.Services;

public class ToDoItemService : IToDoItemService
{
    private readonly HttpClient _httpClient;
    public ToDoItemService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ToDoItemResponseDto> PostToDoItemAsync(CreateToDoItemDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ToDoItem/postToDoItem", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ToDoItemResponseDto>();
        return result;
    }

    public async Task<ToDoItemResponseDto> PutToDoItemAsync(UpdateToDoItemDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync("api/ToDoItem/putToDoItem", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ToDoItemResponseDto>();
        return result;
    }
}
