
using System.Net;
using OneOf;
using OneOf.Types;
using ToDo.Webapp.Dto;

namespace ToDo.Webapp.Services;

/// <summary>
/// Provides methods to create, update, and delete to-do items via an HTTP API.
/// </summary>
/// <remarks>Uses an injected HttpClient to call endpoints under api/ToDoItem. Post and Put operations return
/// OneOf<ToDoItemResponseDto, List<ErrorResponseDto>> and append an ErrorResponseDto with code "http.error" containing
/// the HTTP status code when the response is not successful. Delete is not implemented and throws
/// NotImplementedException.</remarks>
public class ToDoItemService : IToDoItemService
{
    private readonly HttpClient _httpClient;

    public ToDoItemService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Creates a to-do item by posting the provided DTO to the API and returns the created item or a list of errors.
    /// </summary>
    /// <remarks>Sends a POST to "api/ToDoItem/postToDoItem" and deserializes the response as
    /// OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>.</remarks>
    /// <param name="dto">To-do item data to create.</param>
    /// <returns>A OneOf containing the created ToDoItemResponseDto on success, or a List<ErrorResponseDto> on failure. On HTTP
    /// failure an ErrorResponseDto with code "http.error" and a message containing the status code is appended to the
    /// list.</returns>
    public async Task<OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>> PostToDoItemAsync(CreateToDoItemDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ToDoItem/postToDoItem", dto);
        var result = await response.Content.ReadFromJsonAsync<OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>>();

        if (result.IsT0)
            return result.AsT0;
        else
        {
            result.AsT1.Add(new ErrorResponseDto("http.error", $"Request failed with status {(int)response.StatusCode}."));
            return result.AsT1;
        }
    }

    /// <summary>
    /// Update a ToDo item by sending an HTTP PUT with the supplied DTO to the API.
    /// </summary>
    /// <remarks>Sends a PUT request to 'api/ToDoItem/putToDoItem' serializing the DTO as JSON.
    /// HttpRequestException or deserialization exceptions may be thrown.</remarks>
    /// <param name="dto">UpdateToDoItemDto containing the updated to-do item data.</param>
    /// <returns>A task that resolves to either a ToDoItemResponseDto (on success) or a List<ErrorResponseDto> (on failure). On
    /// non-success HTTP status an ErrorResponseDto with code 'http.error' is appended to the returned list.</returns>
    public async Task<OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>> PutToDoItemAsync(UpdateToDoItemDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync("api/ToDoItem/putToDoItem", dto);
        var result = await response.Content.ReadFromJsonAsync<OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>>();
        
        if (result.IsT0)
            return result.AsT0;
        else
        {
            result.AsT1.Add(new ErrorResponseDto("http.error", $"Request failed with status {(int)response.StatusCode}."));
            return result.AsT1;
        }
    }

    /// <summary>
    /// Deletes a ToDo item by its ID. If the item is not found (404), it will return Success to indicate that the item is already deleted.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Returns either a success response or a list of error responses</returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<OneOf<Success, List<ErrorResponseDto>>> DeleteToDoItemAsync(Guid id)
    {
        throw new NotImplementedException("Backend for DeleteToDoItemAsync has not been implemented yet.");
        var response = await _httpClient.DeleteAsync($"api/ToDoItem/deleteToDoItem/{id}");
        var result = await response.Content.ReadFromJsonAsync<OneOf<Success, List<ErrorResponseDto>>>();

        if (result.IsT0)
            return result.AsT0;
        else if (response.StatusCode == HttpStatusCode.NotFound) // Handle 404 Not Found assuming the item is already deleted
            return result.AsT0; // Return Success for 404 Not Found to indicate the item is already deleted
        else
        {
            result.AsT1.Add(new ErrorResponseDto("http.error", $"Request failed with status {(int)response.StatusCode}."));
            return result.AsT1;
        }
    }
}
