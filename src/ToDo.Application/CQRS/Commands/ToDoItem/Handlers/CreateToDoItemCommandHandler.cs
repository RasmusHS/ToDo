using AutoMapper;
using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoItem;
using ToDo.Application.Errors;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.CQRS.Commands.ToDoItem.Handlers;

public class CreateToDoItemCommandHandler : ICreateToDoItemCommand
{
    private readonly ToDoDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateToDoItemCommandHandler(ToDoDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<OneOf<ToDoItemResponseDto, List<ErrorResponseDto>>> CreateAsync(CreateToDoItemDto dto)
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>(); // Initialize an empty list to hold error responses

        try
        {
            var toDoItem = _mapper.Map<ToDoItemEntity>(dto);

            await _dbContext.ToDoItems.AddAsync(toDoItem);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<ToDoItemResponseDto>(toDoItem);
        }
        catch (Exception)
        {
            errors.Add(ToDoItemErrors.InvalidData<CreateToDoItemDto>());
            return errors;
        }
    }
}
