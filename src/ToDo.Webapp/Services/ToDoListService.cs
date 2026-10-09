using ToDo.Webapp.Dto.Queries.ToDoList;

namespace ToDo.Webapp.Services;

public class ToDoListService : IToDoListService
{
    private readonly HttpClient _httpClient;
    public ToDoListService(HttpClient httpClient)
    {
        _httpClient = httpClient;    
    }

    /// <summary>
    /// Gets all ToDoListEntities from the API without their associated ToDoItemEntities.
    /// </summary>
    /// <returns></returns>
    public async Task<List<QueryToDoListSummaryDto>> GetAllToDoListsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<QueryToDoListSummaryDto>>("api/ToDoList/getAllToDoLists");
        return response;
    }

    /// <summary>
    /// Gets a ToDoListEntity by its unique identifier (id) from the API and returns its details including associated ToDoItemEntities.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<QueryToDoListDto> GetToDoListAsync(Guid id)
    {
        var response = await _httpClient.GetFromJsonAsync<QueryToDoListDto>($"api/ToDoList/getToDoList/{id}");
        return response;
    }

    /// <summary>
    /// Posts a new ToDoListEntity to the API and returns the created entity's details.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<ToDoListResponseDto> PostToDoListAsync(CreateToDoListDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ToDoList/postToDoList", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ToDoListResponseDto>();
        return result;
    }
}
