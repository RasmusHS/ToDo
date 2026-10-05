namespace ToDo.Application.DTO;

public class ErrorResponseDto
{
    // Wrapper for error messages defined in the application layer. This class is used to return error messages to the client in a consistent format.
    public ErrorResponseDto(string errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
