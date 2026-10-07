using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Queries;
using ToDo.Application.Errors;
using ToDo.Persistence;

namespace ToDo.Application.CQRS.Queries.ToDoList.Handlers;

public class GetToDoListQueryHandler : IGetToDoListQuery
{
    private readonly ToDoDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetToDoListQueryHandler(ToDoDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<OneOf<QueryToDoListDto, List<ErrorResponseDto>>> GetAsync(Guid id)
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>();

        try
        {
            var result = await _dbContext.ToDoLists
                .Include(tdl => tdl.ToDoItems)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (result == null)
            {
                errors.Add(ToDoListErrors.NotFound<QueryToDoListDto>(id));
                return errors;
            }

            return _mapper.Map<QueryToDoListDto>(result);
        }
        catch (Exception)
        {
            errors.Add(ToDoListErrors.InvalidData<QueryToDoListDto>());
            return errors;
        }
    }
}
