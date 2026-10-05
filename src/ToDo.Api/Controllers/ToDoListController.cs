using Microsoft.AspNetCore.Mvc;
using ToDo.Application.CQRS.Commands.ToDoList;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoList;

namespace ToDo.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ToDoListController : ControllerBase
{
    private readonly ICreateToDoListCommand _createToDoList;

    public ToDoListController(ICreateToDoListCommand createToDoList)
    {
        _createToDoList = createToDoList;
    }

    [HttpPost]
    [Route("postToDoList")]
    public async Task<IActionResult> PostToDoList(CreateToDoListDto dto)
    {
        CreateToDoListDto.Validator validator = new CreateToDoListDto.Validator();
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

        var commandResult = await _createToDoList.CreateAsync(dto);

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
