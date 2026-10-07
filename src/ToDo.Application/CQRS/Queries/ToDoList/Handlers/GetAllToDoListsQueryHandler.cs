using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OneOf;
using ToDo.Application.DTO;
using ToDo.Application.DTO.Queries;
using ToDo.Application.Errors;
using ToDo.Persistence;

namespace ToDo.Application.CQRS.Queries.ToDoList.Handlers;

public class GetAllToDoListsQueryHandler : IGetAllToDoListsQuery
{
    private readonly ToDoDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetAllToDoListsQueryHandler(ToDoDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<OneOf<List<QueryToDoListDto>, List<ErrorResponseDto>>> GetAllAsync()
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>();

        var result = await _dbContext.ToDoLists.ToListAsync();

        if (!result.Any())
        {
            errors.Add(ToDoListErrors.NotFoundCollection());
            return errors;
        }

        return _mapper.Map<List<QueryToDoListDto>>(result);
    }
}
