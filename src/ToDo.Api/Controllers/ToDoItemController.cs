using Microsoft.AspNetCore.Mvc;
using ToDo.Application.CQRS.Commands.ToDoItem;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoItem;

namespace ToDo.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ToDoItemController : ControllerBase
{
    private readonly ICreateToDoItemCommand _createToDoItem;

    public ToDoItemController(ICreateToDoItemCommand createToDoItem)
    {
        _createToDoItem = createToDoItem;
    }

    [HttpPost]
    [Route("postToDoItem")]
    public async Task<IActionResult> PostToDoItem(CreateToDoItemDto dto)
    {
        CreateToDoItemDto.Validator validator = new CreateToDoItemDto.Validator();
        var result = await validator.ValidateAsync(dto);
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>();

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                errors.Add(new ErrorResponseDto(error.ErrorCode, error.ErrorMessage));
            }
        }

        if (errors.Count > 0)
        {
            return BadRequest(errors);
        }

        var commandResult = await _createToDoItem.CreateAsync(dto);

        if (commandResult.IsT0)
        {
            return Ok(commandResult.AsT0);

        }
        else
        {
            return BadRequest(commandResult.AsT1);
        }
    }
}
