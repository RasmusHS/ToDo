
public class ToDoListService : IToDoListService
{
    private readonly HttpClient _httpClient;
    public ToDoListService(HttpClient httpClient)
    {
        _httpClient = httpClient;    
    }

    public async Task<List<QueryToDoListDto>> GetAllToDoListsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<QueryToDoListDto>>("api/ToDoList/getAllToDoLists");
        return response;
    }

    public Task<QueryToDoListDto> GetToDoListAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<ToDoListResponseDto> PostToDoListAsync(CreateToDoListDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ToDoList/postToDoList", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ToDoListResponseDto>();
        return result;
    }
}