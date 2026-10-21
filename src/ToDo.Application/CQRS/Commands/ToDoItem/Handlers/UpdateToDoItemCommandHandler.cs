using AutoMapper;
using OneOf;
using ToDo.Application.CQRS.Commands.ToDoItem;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoItem;
using ToDo.Application.Errors;
using ToDo.Persistence;

public class UpdateToDoItemCommandHandler : IUpdateToDoItemCommand
{
    private readonly ToDoDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateToDoItemCommandHandler(ToDoDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>> UpdateAsync(UpdateToDoItemDto dto)
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>(); // Initialize an empty list to hold error responses

        var result = await _dbContext.ToDoItems.FindAsync(dto.Id);

        if(result == null)
        {
            errors.Add(ToDoItemErrors.NotFound<UpdateToDoItemDto>(dto.Id));
            return errors;
        }

        result.Update(dto.IsDone, dto.Text, dto.Status);

        _dbContext.ToDoItems.Update(result);
        await _dbContext.SaveChangesAsync();

        return _mapper.Map<ToDoItemResponseDto>(result);
    }
}
