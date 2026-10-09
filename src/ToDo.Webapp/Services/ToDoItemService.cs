using ToDo.Webapp.Dto.Queries.ToDoItem;

namespace ToDo.Webapp.Services;

public class ToDoItemService : IToDoItemService
{
    private readonly HttpClient _httpClient;
    public ToDoItemService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    // TODO: Remove endpoint from API controller and use the GetToDoListAsync method in ToDoListService to get the items for a list.
    public async Task<List<QueryToDoItemDto>> GetToDoItemsFromListAsync(Guid listId)
    {
        var response = await _httpClient.GetFromJsonAsync<List<QueryToDoItemDto>>($"api/ToDoItem/getToDoItemsFromList{listId}");
        return response;
    }

    public async Task<ToDoItemResponseDto> PostToDoItemAsync(CreateToDoItemDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ToDoItem/postToDoItem", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ToDoItemResponseDto>();
        return result;
    }
}
