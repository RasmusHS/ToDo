using ToDo.Application.DTO;

namespace ToDo.Application.Errors;

public static class ToDoItemErrors
{
    public static ErrorResponseDto NotFound<T>() => new ErrorResponseDto("entity.not.found", $"{typeof(T).Name} not found.");

    // Create errors
    public static ErrorResponseDto InvalidData<T>() => new ErrorResponseDto("entity.invalid.data", $"{typeof(T).Name} has invalid data.");
}
