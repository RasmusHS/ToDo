using Microsoft.AspNetCore.Mvc;
using ToDo.Application.CQRS.Commands.ToDoList;
using ToDo.Application.CQRS.Queries.ToDoList;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoList;
using ToDo.Application.DTO.Queries;
using ToDo.Application.Errors;

namespace ToDo.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ToDoListController : ControllerBase
{
    private readonly ICreateToDoListCommand _createToDoList;
    private readonly IGetAllToDoListsQuery _getAllToDoLists;
    private readonly IGetToDoListQuery _getToDoList;

    public ToDoListController(ICreateToDoListCommand createToDoList)
    {
        _createToDoList = createToDoList;
        _getAllToDoLists = getAllToDoLists;
        _getToDoList = getToDoList;
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
            return BadRequest(errors);

        var commandResult = await _createToDoList.CreateAsync(dto);

        if (commandResult.IsT0)
            return Ok(commandResult.AsT0);
        else
            return BadRequest(commandResult.AsT1);
    }

    [HttpGet]
    [Route("getToDoList/{id}")]
    public async Task<IActionResult> GetToDoList(Guid id)
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>();

        if (id == Guid.Empty)
            errors.Add(ToDoListErrors.InvalidData<QueryToDoListDto>());

        if (errors.Count > 0)
            return BadRequest(errors);

        var queryResult = await _getToDoList.GetAsync(id);

        if (queryResult.IsT0)
            return Ok(queryResult.AsT0);
        else
            return NotFound(queryResult.AsT1);
    }

    [HttpGet]
    [Route("getAllToDoLists")]
    public async Task<IActionResult> GetAllToDoLists()
    {
        var queryResult = await _getAllToDoLists.GetAllAsync();

        if (queryResult.IsT0)
            return Ok(queryResult.AsT0);
        else
            return NotFound(queryResult.AsT1);
    }


}
