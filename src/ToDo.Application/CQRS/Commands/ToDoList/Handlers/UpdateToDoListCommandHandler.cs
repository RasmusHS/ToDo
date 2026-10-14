using AutoMapper;
using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoList;
using ToDo.Application.Errors;
using ToDo.Persistence;

namespace ToDo.Application.CQRS.Commands.ToDoList.Handlers;

public class UpdateToDoListCommandHandler : IUpdateToDoListCommand
{
    private readonly ToDoDbContext _dbContext;
    private readonly IMapper _mapper;
    public UpdateToDoListCommandHandler(ToDoDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }
    public async Task<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>> UpdateAsync(UpdateToDoListDto dto)
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>(); // Initialize an empty list to hold error responses

        if(!_dbContext.ToDoLists.Any(t => t.Id == dto.Id))
        {
            errors.Add(ToDoListErrors.NotFound<UpdateToDoListDto>(dto.Id));
        }

        if(_dbContext.ToDoLists.Any(t => t.ListTitle == dto.ListTitle))
        {
            errors.Add(ToDoListErrors.AlreadyExists<UpdateToDoListDto>(dto.ListTitle));
        }

        if(errors.Count() > 0)
        {
            return errors;
        }

        var result = await _dbContext.ToDoLists.FindAsync(dto.Id);

        result.Update(dto.ListTitle, dto.ListDescription);

        _dbContext.ToDoLists.Update(result);
        await _dbContext.SaveChangesAsync();

        return _mapper.Map<ToDoListResponseDto>(result);
        

    }
}