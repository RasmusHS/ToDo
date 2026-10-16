using ToDo.Application.DTO;

namespace ToDo.Application.Errors;

public static class ToDoItemErrors
{
    public static ErrorResponseDto NotFound<T>(Guid id) => new ErrorResponseDto("entity.not.found", $"{typeof(T).Name} with ID {id} not found.");
    public static ErrorResponseDto NotFoundInCollection<T>(Guid id) => new ErrorResponseDto("entity.not.found.in.collection", $"{typeof(T).Name} with ID {id} not found in the specified collection.");

    // Create errors
    public static ErrorResponseDto InvalidData<T>() => new ErrorResponseDto("entity.invalid.data", $"{typeof(T).Name} has invalid data.");
}
