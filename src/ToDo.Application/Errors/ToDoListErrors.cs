using ToDo.Application.DTO;

namespace ToDo.Application.Errors;

public static class ToDoListErrors
{
    public static ErrorResponseDto NotFound<T>(Guid id) => new ErrorResponseDto("entity.not.found", $"{typeof(T).Name} with ID {id} not found.");

    // Create errors 
    public static ErrorResponseDto AlreadyExists<T>(string identifier) => new ErrorResponseDto("entity.already.exists", $"{typeof(T).Name} '{identifier}' already exists.");
    public static ErrorResponseDto InvalidData<T>() => new ErrorResponseDto("entity.invalid.data", $"{typeof(T).Name} has invalid data.");

}
