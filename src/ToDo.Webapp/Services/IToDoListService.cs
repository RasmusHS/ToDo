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
public interface IToDoListService
{
    /// <summary>
    /// Posts a new ToDoListEntity to the API and returns the created entity's details.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    Task<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>> PostToDoListAsync(CreateToDoListDto dto);

    /// <summary>
    /// Gets a ToDoListEntity by its unique identifier (id) from the API and returns its details including associated ToDoItemEntities.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<OneOf<QueryToDoListDto, List<ErrorResponseDto>>> GetToDoListAsync(Guid id);

    /// <summary>
    /// Gets all ToDoListEntities from the API without their associated ToDoItemEntities.
    /// </summary>
    /// <returns></returns>
    Task<OneOf<List<QueryToDoListSummaryDto>, List<ErrorResponseDto>>> GetAllToDoListsAsync();

    /// <summary>
    /// Updates title and/or description of a given ToDoList
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    Task<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>> PutToDoListAsync(UpdateToDoListDto dto);

    /// <summary>
    /// Deletes a ToDoListEntity by its unique identifier (id) from the API and returns a success response (without associated entities) or a list of error responses if the deletion fails.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<OneOf<Success, List<ErrorResponseDto>>> DeleteToDoListAsync(Guid id);
    
}
