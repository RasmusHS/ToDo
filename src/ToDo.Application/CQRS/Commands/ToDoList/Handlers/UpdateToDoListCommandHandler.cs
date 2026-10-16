using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

        var result = await _dbContext.ToDoLists.FindAsync(dto.Id);

        if (result == null)
        {
            errors.Add(ToDoListErrors.NotFound<UpdateToDoListDto>(dto.Id));
            return errors;
        }

        if (await _dbContext.ToDoLists.AnyAsync(t => t.ListTitle == dto.ListTitle && t.Id != dto.Id))
        {
            errors.Add(ToDoListErrors.AlreadyExists<UpdateToDoListDto>(dto.ListTitle));
            return errors;
        }

        result.Update(dto.ListTitle, dto.ListDescription);

        _dbContext.ToDoLists.Update(result);
        await _dbContext.SaveChangesAsync();

        return _mapper.Map<ToDoListResponseDto>(result);
    }
}
