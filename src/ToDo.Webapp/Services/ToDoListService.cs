using System.Net;
using OneOf;
using OneOf.Types;
using ToDo.Webapp.Dto;
using ToDo.Webapp.Dto.Queries.ToDoList;

namespace ToDo.Webapp.Services;

/// <summary>
/// Provides methods to create, retrieve, update, and delete to-do lists via an injected HttpClient.
/// </summary>
/// <remarks>Methods return OneOf wrappers containing DTOs or a list of ErrorResponseDto; HTTP failures append an
/// ErrorResponseDto with code "http.error". HttpClient should be configured (base address, headers) and supplied by
/// dependency injection.</remarks>
public class ToDoListService : IToDoListService
{
    private readonly HttpClient _httpClient;

    public ToDoListService(HttpClient httpClient)
    {
        _httpClient = httpClient;    
    }

    /// <summary>
    /// Posts a new ToDoListEntity to the API and returns the created entity's details.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>Returns either the created ToDoList or a list of error responses</returns>
    public async Task<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>> PostToDoListAsync(CreateToDoListDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ToDoList/postToDoList", dto);
        var result = await response.Content.ReadFromJsonAsync<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>>();

        if (result.IsT0)
            return result.AsT0;
        else
        {
            result.AsT1.Add(new ErrorResponseDto("http.error", $"Request failed with status {(int)response.StatusCode}."));
            return result.AsT1;
        }
    }

    /// <summary>
    /// Gets a ToDoListEntity by its unique identifier (id) from the API and returns its details including associated ToDoItemEntities.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Returns either the ToDoList or a list of error responses</returns>
    public async Task<OneOf<QueryToDoListDto, List<ErrorResponseDto>>> GetToDoListAsync(Guid id)
    {
        var response = await _httpClient.GetFromJsonAsync<OneOf<QueryToDoListDto, List<ErrorResponseDto>>>($"api/ToDoList/getToDoList/{id}");

        if (response.IsT0)
            return response;
        else
        {
            response.AsT1.Add(new ErrorResponseDto("http.error", $"Request failed. Could not retrieve ToDoList with ID {id}."));
            return response.AsT1;
        }
    }

    /// <summary>
    /// Gets all ToDoListEntities from the API without their associated ToDoItemEntities.
    /// </summary>
    /// <returns>Returns either a list of ToDoLists or a list of error responses</returns>
    public async Task<OneOf<List<QueryToDoListSummaryDto>, List<ErrorResponseDto>>> GetAllToDoListsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<OneOf<List<QueryToDoListSummaryDto>, List<ErrorResponseDto>>>("api/ToDoList/getAllToDoLists");

        if (response.IsT0)
            return response.AsT0;
        else
        {
            response.AsT1.Add(new ErrorResponseDto("http.error", "Request failed. Could not retrieve any ToDoLists."));
            return response.AsT1;
        }
    }

    /// <summary>
    /// Updates title and/or description of a given ToDoList
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>Returns either the updated ToDoList or a list of error responses</returns>
    public async Task<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>> PutToDoListAsync(UpdateToDoListDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync("api/ToDoList/putToDoList", dto);
        var result = await response.Content.ReadFromJsonAsync<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>>();

        if (result.IsT0)
            return result.AsT0;
        else
        {
            result.AsT1.Add(new ErrorResponseDto("http.error", $"Request failed with status {(int)response.StatusCode}."));
            return result.AsT1;
        }
    }

    /// <summary>
    /// Deletes a ToDoListEntity by its unique identifier (id) from the API and returns a success response (without associated entities) or a list of error responses if the deletion fails.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Returns either a success response or a list of error responses</returns>
    public async Task<OneOf<Success, List<ErrorResponseDto>>> DeleteToDoListAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/ToDoList/deleteToDoList/{id}");
        var result = await response.Content.ReadFromJsonAsync<OneOf<Success, List<ErrorResponseDto>>>();

        if (result.IsT0 || response.StatusCode == HttpStatusCode.NoContent)
            return result.AsT0;
        else if (response.StatusCode == HttpStatusCode.NotFound)
            return result.AsT0;
        else 
        {
            result.AsT1.Add(new ErrorResponseDto("http.error", $"Request failed with status {(int)response.StatusCode}."));
            return result.AsT1;
        }
    }
}
