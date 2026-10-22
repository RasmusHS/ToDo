using ToDo.Application.DTO;

namespace ToDo.Application.Errors;

public static class ToDoItemErrors
{
    public static ErrorResponseDto NotFound(Guid id) => new ErrorResponseDto("entity.not.found", $"ToDoItemEntity with ID {id} not found.");
    public static ErrorResponseDto NotFound<T>(Guid id) => new ErrorResponseDto("entity.not.found", $"{typeof(T).Name} with ID {id} not found.");
}
