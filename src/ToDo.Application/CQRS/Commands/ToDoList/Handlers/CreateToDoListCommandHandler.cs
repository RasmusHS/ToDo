using AutoMapper;
using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Commands.ToDoList;
using ToDo.Application.Errors;
using ToDo.Domain;
using ToDo.Persistence;

namespace ToDo.Application.CQRS.Commands.ToDoList.Handlers;

public class CreateToDoListCommandHandler : ICreateToDoListCommand
{
    private readonly ToDoDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateToDoListCommandHandler(ToDoDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<OneOf<ToDoListResponseDto, List<ErrorResponseDto>>> CreateAsync(CreateToDoListDto dto)
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>(); // Initialize an empty list to hold error responses

        try
        {
            if (_dbContext.ToDoLists.Any(t => t.ListTitle == dto.ListTitle))
            {
                errors.Add(ToDoListErrors.AlreadyExists<CreateToDoListDto>(dto.ListTitle));
                return errors; // Return the list of errors if a ToDoList with the same title already exists
            }   

            var toDoListEntity = _mapper.Map<ToDoListEntity>(dto);

            await _dbContext.ToDoLists.AddAsync(toDoListEntity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<ToDoListResponseDto>(toDoListEntity);
        }
        catch (Exception) 
        {
            // Return an error response DTO with the exception message
            errors.Add(ToDoListErrors.InvalidData<CreateToDoListDto>());
            return errors;
        }
    }
}
