
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
public interface IToDoItemService
{
    /// <summary>
    /// Creates a to-do item by posting the provided DTO to the API and returns the created item or a list of errors.
    /// </summary>
    /// <remarks>Sends a POST to "api/ToDoItem/postToDoItem" and deserializes the response as
    /// OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>.</remarks>
    /// <param name="dto">To-do item data to create.</param>
    /// <returns>A OneOf containing the created ToDoItemResponseDto on success, or a List<ErrorResponseDto> on failure. On HTTP
    /// failure an ErrorResponseDto with code "http.error" and a message containing the status code is appended to the
    /// list.</returns>
    Task<OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>> PostToDoItemAsync(CreateToDoItemDto dto);

    /// <summary>
    /// Update a ToDo item by sending an HTTP PUT with the supplied DTO to the API.
    /// </summary>
    /// <remarks>Sends a PUT request to 'api/ToDoItem/putToDoItem' serializing the DTO as JSON.
    /// HttpRequestException or deserialization exceptions may be thrown.</remarks>
    /// <param name="dto">UpdateToDoItemDto containing the updated to-do item data.</param>
    /// <returns>A task that resolves to either a ToDoItemResponseDto (on success) or a List<ErrorResponseDto> (on failure). On
    /// non-success HTTP status an ErrorResponseDto with code 'http.error' is appended to the returned list.</returns>
    Task<OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>> PutToDoItemAsync(UpdateToDoItemDto dto);

    /// <summary>
    /// Deletes a ToDo item by its ID. If the item is not found (404), it will return Success to indicate that the item is already deleted.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Returns either a success response or a list of error responses</returns>
    /// <exception cref="NotImplementedException"></exception>
    Task<OneOf<Success, List<ErrorResponseDto>>> DeleteToDoItemAsync(Guid id);
}
