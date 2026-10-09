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

    /// <summary>
    /// Gets all ToDo lists, returning a summary of each list.
    /// </summary>
    /// <inheritdoc cref="IGetAllToDoListsQuery.GetAllAsync"/>
    /// <returns></returns>
    public async Task<OneOf<List<QueryToDoListSummaryDto>, List<ErrorResponseDto>>> GetAllAsync()
    {
        List<ErrorResponseDto> errors = new List<ErrorResponseDto>();

        var result = await _mapper
            .ProjectTo<QueryToDoListSummaryDto>(_dbContext.ToDoLists)
            .ToListAsync();

        if (!result.Any())
        {
            errors.Add(ToDoListErrors.NotFoundCollection());
            return errors;
        }

        return result;
    }
}
