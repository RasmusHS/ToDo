using Microsoft.AspNetCore.Mvc;
using ToDo.Application.CQRS.Commands.ToDoItem;
using ToDo.Application.CQRS.Queries.ToDoItem;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoItem;
using ToDo.Application.DTO.Queries;
using ToDo.Application.Errors;

namespace ToDo.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ToDoItemController : ControllerBase
{
    private readonly ICreateToDoItemCommand _createToDoItem;

    private readonly IGetToDoItemsFromListQuery _getToDoItemsFromList;

    public ToDoItemController(ICreateToDoItemCommand createToDoItem, IGetToDoItemsFromListQuery getToDoItemsFromList)
    {
        _createToDoItem = createToDoItem;
        _getToDoItemsFromList = getToDoItemsFromList;
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
    [HttpGet]
    [Route("getTodoItemsFromList/{id}")]
    public async Task<IActionResult> GetToDoItemsFromList(Guid id)
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>();

        if (id == Guid.Empty)
            errors.Add(ToDoItemErrors.InvalidData<QueryToDoItemDto>());

        if (errors.Count > 0)
            return BadRequest(errors);

        var queryResult = await _getToDoItemsFromList.GetAsync(id);

        if (queryResult.IsT0)
            return Ok(queryResult.AsT0);
        else
            return NotFound(queryResult.AsT1);
    }
}
